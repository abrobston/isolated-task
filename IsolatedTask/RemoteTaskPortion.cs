using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Text;

namespace IsolatedTask
{
    [Serializable]
    internal class RemoteTaskPortion
    {
        private ObjRef m_isolatedTaskObjRef;
        private IsolatedTask m_unmarshalled;
        private TaskLoggingHelper m_log;
        private IBuildEngine m_buildEngine;
        private ITaskHost m_hostObject;
        private AssemblyName m_determinedAssemblyName;
        private string m_outputParameterName;
        private string m_taskNameWithNamespace;
        private string[] m_parameterNames;
        private string[] m_parameterValues;
        private ITaskItem[] m_taskItems;
        private bool m_inLoadEvent;

        public RemoteTaskPortion(ObjRef isolatedTaskObjRef)
        {
            if (isolatedTaskObjRef == null)
            {
                throw new ArgumentNullException("isolatedTaskObjRef");
            }

            m_isolatedTaskObjRef = isolatedTaskObjRef;
        }

        public void Execute()
        {
            try
            {
                if (m_isolatedTaskObjRef != null)
                {
                    m_unmarshalled = (IsolatedTask)RemotingServices.Unmarshal(m_isolatedTaskObjRef, true);
                    m_log = m_unmarshalled.Log;
                    m_buildEngine = m_unmarshalled.BuildEngine;
                    m_hostObject = m_unmarshalled.HostObject;
                    m_determinedAssemblyName = m_unmarshalled.DeterminedAssemblyName;
                    m_outputParameterName = m_unmarshalled.OutputParameterName;
                    m_taskNameWithNamespace = m_unmarshalled.TaskNameWithNamespace;
                    m_parameterNames = m_unmarshalled.ParameterNames;
                    m_parameterValues = m_unmarshalled.ParameterValues;
                    m_taskItems = m_unmarshalled.TaskItems;
                }

                m_inLoadEvent = false;
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                AppDomain.CurrentDomain.TypeResolve += CurrentDomain_TypeResolve;
                try
                {
                    AppDomain.CurrentDomain.Load(m_determinedAssemblyName);
                }
                catch (Exception)
                {
                    m_log.LogErrorFromResources("ErrorAssemblyLoadFailed", m_determinedAssemblyName.FullName, AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.BaseDirectory);
                    throw;
                }

                object typeInstance = AppDomain.CurrentDomain.CreateInstanceAndUnwrap(m_determinedAssemblyName.FullName, m_taskNameWithNamespace);
                if (typeInstance == null)
                {
                    m_log.LogErrorFromResources("ErrorTypeNotFound", m_taskNameWithNamespace, m_determinedAssemblyName.FullName);
                    return;
                }

                Type taskType = typeInstance.GetType();

                if (!typeof(ITask).IsAssignableFrom(taskType))
                {
                    m_log.LogErrorFromResources("ErrorTypeNotITask", taskType.FullName, m_determinedAssemblyName.FullName, typeof(ITask).Assembly.FullName);
                    return;
                }

                // Create dictionary of property names to values, all as strings for now.
                // Support arrays by permitting multiple parameters to have the same name.
                Dictionary<string, List<string>> parameters = new Dictionary<string, List<string>>();
                int parameterCount = m_parameterNames.Length;
                for (int index = 0; index < parameterCount; index++)
                {
                    List<string> valueList;
                    if (parameters.TryGetValue(m_parameterNames[index], out valueList))
                    {
                        valueList.Add(m_parameterValues[index]);
                    }
                    else
                    {
                        parameters.Add(m_parameterNames[index], new List<string>() { m_parameterValues[index] });
                    }
                }

                // Verify properties.  Only include properties with a public getter and setter.
                Dictionary<string, object> convertedProperties = new Dictionary<string, object>();
                foreach (var property in taskType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetAccessors(false).Length == 2))
                {
                    // Ensure that the property is not a hidden, inherited property
                    string name = property.Name;
                    if (taskType.IsSubclassOf(property.DeclaringType) &&
                        taskType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.Name == name && p.DeclaringType.IsSubclassOf(property.DeclaringType)))
                    {
                        m_log.LogMessageFromResources("MessageSkippingHiddenProperty", name, property.DeclaringType.FullName);
                        continue;
                    }

                    bool output = Attribute.IsDefined(property, typeof(OutputAttribute), true);
                    if (output)
                    {
                        continue;
                    }

                    bool required = Attribute.IsDefined(property, typeof(RequiredAttribute), true);
                    Type propertyType = property.PropertyType;
                    bool isArray = propertyType.IsArray;
                    Type elementType = isArray ? propertyType.GetElementType() : propertyType;

                    List<string> parameterValues;
                    if (parameters.TryGetValue(name, out parameterValues))
                    {
                        if (elementType.Equals(typeof(string)))
                        {
                            if (isArray)
                            {
                                convertedProperties.Add(name, parameterValues.ToArray());
                            }
                            else
                            {
                                convertedProperties.Add(name, parameterValues.First());
                            }
                        }
                        else
                        {
                            var converter = TypeDescriptor.GetConverter(elementType);
                            if (!converter.CanConvertFrom(typeof(string)))
                            {
                                if (required)
                                {
                                    m_log.LogErrorFromResources("ErrorCannotConvertFromString", name, propertyType.FullName);
                                }
                                else
                                {
                                    m_log.LogWarningFromResources("WarningCannotConvertFromString", name, propertyType.FullName);
                                }
                            }
                            else
                            {
                                try
                                {
                                    var valueArray = parameterValues.Select(v => converter.ConvertFromString(v)).ToArray();
                                    if (isArray)
                                    {
                                        var typedArray = Array.CreateInstance(elementType, valueArray.Length);
                                        valueArray.CopyTo(typedArray, 0);
                                        convertedProperties.Add(name, typedArray);
                                    }
                                    else
                                    {
                                        var firstValue = converter.ConvertFromString(parameterValues.First());
                                        convertedProperties.Add(name, Convert.ChangeType(firstValue, elementType));
                                    }
                                }
                                catch (Exception)
                                {
                                    if (required)
                                    {
                                        m_log.LogErrorFromResources("ErrorConversionFailed", propertyType.FullName, name, String.Join(";", parameterValues.ToArray()));
                                    }
                                    else
                                    {
                                        m_log.LogWarningFromResources("WarningConversionFailed", propertyType.FullName, name, String.Join(";", parameterValues.ToArray()));
                                    }
                                }
                            }
                        }
                    }
                    else if (elementType.IsAssignableFrom(typeof(ITaskItem)))
                    {
                        if (isArray)
                        {
                            convertedProperties.Add(name, m_taskItems ?? new ITaskItem[0]);
                        }
                        else if (m_taskItems != null && m_taskItems.Length > 0)
                        {
                            convertedProperties.Add(name, m_taskItems[0]);
                        }
                        else if (required)
                        {
                            m_log.LogErrorFromResources("ErrorMissingRequiredParameter", name);
                        }
                    }
                    else if (isArray)
                    {
                        convertedProperties.Add(name, Array.CreateInstance(elementType, 0));
                        m_log.LogMessageFromResources(MessageImportance.Low, "MessageUsingDefaultEmptyArray", name);
                    }
                    else if (required)
                    {
                        m_log.LogErrorFromResources("ErrorMissingRequiredParameter", name);
                    }
                }

                if (m_log.HasLoggedErrors)
                {
                    return;
                }

                var task = (ITask)typeInstance;
                task.BuildEngine = m_buildEngine;
                task.HostObject = m_hostObject;
                foreach (var kvp in convertedProperties)
                {
                    taskType.GetProperty(kvp.Key, BindingFlags.Instance | BindingFlags.Public).GetSetMethod(false).Invoke(task, new object[] { kvp.Value });
                }
                bool result = task.Execute();
                if (!result)
                {
                    m_log.LogErrorFromResources("ErrorInnerTaskFailed", taskType.FullName, m_determinedAssemblyName.FullName);
                }
                else if (!String.IsNullOrEmpty(m_outputParameterName))
                {
                    var outputProperty = taskType.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.Name == m_outputParameterName && p.IsDefined(typeof(OutputAttribute), true) && p.GetAccessors(false).Length == 2);
                    if (outputProperty != null)
                    {
                        var outputValue = outputProperty.GetGetMethod().Invoke(task, null);
                        if (outputValue != null)
                        {
                            if (outputProperty.PropertyType.IsArray)
                            {
                                Type outputElementType = outputProperty.PropertyType.GetElementType();
                                m_unmarshalled.InnerTaskOutput = String.Join(";", ((Array)outputValue).OfType<object>().Select(v => Convert.ChangeType(v, outputElementType).ToString()).ToArray());
                            }
                            else
                            {
                                m_unmarshalled.InnerTaskOutput = Convert.ChangeType(outputValue, outputProperty.PropertyType).ToString();
                            }
                        }
                    }
                }
            }
            finally
            {
                AppDomain.CurrentDomain.TypeResolve -= CurrentDomain_TypeResolve;
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        private Assembly CurrentDomain_TypeResolve(object sender, ResolveEventArgs args)
        {
            if (m_inLoadEvent)
            {
                // Prevent infinite recursion
                return null;
            }

            try
            {
                m_inLoadEvent = true;
                if (args.Name.Equals(m_taskNameWithNamespace, StringComparison.InvariantCultureIgnoreCase))
                {
                    return Assembly.Load(m_determinedAssemblyName);
                }
                m_log.LogWarningFromResources("ErrorUnableToResolveWithin", LogText.Type, args.Name, "RemoteTaskPortion.CurrentDomain_TypeResolve");
                return null;
            }
            finally
            {
                m_inLoadEvent = false;
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (m_inLoadEvent)
            {
                // Prevent infinite recursion
                return null;
            }

            try
            {
                m_inLoadEvent = true;
                Assembly retVal = null;
                var missingAssemblyName = new AssemblyName(args.Name);
                if (m_determinedAssemblyName.Name.Equals(missingAssemblyName.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    retVal = Assembly.Load(m_determinedAssemblyName);
                }
                m_log.LogWarningFromResources("ErrorUnableToResolveWithin", LogText.Assembly, args.Name, "RemoteTaskPortion.CurrentDomain_AssemblyResolve");
                return retVal;
            }
            finally
            {
                m_inLoadEvent = false;
            }
        }
    }
}
