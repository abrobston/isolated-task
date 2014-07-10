using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Reflection;
using SR = System.Reflection;
using System.ComponentModel;

namespace IsolatedTask
{
    public class IsolatedTask : AppDomainIsolatedTask
    {
        public IsolatedTask()
            : base(LogText.ResourceManager)
        {
        }

        public override bool Execute()
        {
            if (String.IsNullOrEmpty(AssemblyName) && String.IsNullOrEmpty(AssemblyFile))
            {
                Log.LogErrorFromResources("ErrorAssemblyNameOrFileMustBeSet");
            }
            else if (String.IsNullOrEmpty(AssemblyName) == String.IsNullOrEmpty(AssemblyFile))
            {
                Log.LogErrorFromResources("ErrorOnlyOneCanBeSet");
            }

            if (this.ParameterNames == null)
            {
                this.ParameterNames = new string[0];
            }

            if (this.ParameterValues == null)
            {
                this.ParameterValues = new string[0];
            }

            if (ParameterNames.Length != ParameterValues.Length)
            {
                Log.LogErrorFromResources("ErrorParameterNamesAndValuesMustHaveSameLength", ParameterNames.Length, ParameterValues.Length);
            }

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            try
            {

                Assembly assembly = null;
                if (!String.IsNullOrEmpty(this.AssemblyName))
                {
                    SR.AssemblyName assemblyName = new AssemblyName(this.AssemblyName);
                    assembly = Assembly.Load(assemblyName);
                    Log.LogMessageFromResources("MessageAssemblyLoadedFromName", assemblyName.FullName);
                }
                else
                {
                    assembly = Assembly.LoadFrom(this.AssemblyFile);
                    Log.LogMessageFromResources("MessageAssemblyLoadedFromFile", assembly.FullName, this.AssemblyFile);
                }

                Type taskType = assembly.GetType(this.TaskNameWithNamespace, false, true);
                if (taskType == null)
                {
                    Log.LogErrorFromResources("ErrorTypeNotFound", this.TaskNameWithNamespace, assembly.FullName);
                    return false;
                }

                if (taskType.IsInterface)
                {
                    Log.LogErrorFromResources("ErrorTypeIsInterface", taskType.FullName, assembly.FullName);
                }

                if (taskType.IsAbstract)
                {
                    Log.LogErrorFromResources("ErrorTypeIsAbstract", taskType.FullName, assembly.FullName);
                }

                if (!typeof(ITask).IsAssignableFrom(taskType))
                {
                    Log.LogErrorFromResources("ErrorTypeNotITask", taskType.FullName, assembly.FullName, typeof(ITask).Assembly.FullName);
                }

                if (Log.HasLoggedErrors)
                {
                    return false;
                }

                // Create dictionary of property names to values, all as strings for now.
                // Support arrays by permitting multiple parameters to have the same name.
                Dictionary<string, List<string>> parameters = new Dictionary<string, List<string>>();
                int parameterCount = this.ParameterNames.Length;
                for (int index = 0; index < parameterCount; index++)
                {
                    List<string> valueList;
                    if (parameters.TryGetValue(this.ParameterNames[index], out valueList))
                    {
                        valueList.Add(this.ParameterValues[index]);
                    }
                    else
                    {
                        parameters.Add(this.ParameterNames[index], new List<string>() { this.ParameterValues[index] });
                    }
                }

                // Verify properties.  Only include properties with a public getter and setter.
                Dictionary<string, object> convertedProperties = new Dictionary<string, object>();
                foreach (var property in taskType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p =>  p.GetAccessors(false).Length == 2))
                {
                    // Ensure that the property is not a hidden, inherited property
                    string name = property.Name;
                    if (taskType.IsSubclassOf(property.DeclaringType) &&
                        taskType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.Name == name && p.DeclaringType.IsSubclassOf(property.DeclaringType)))
                    {
                        Log.LogMessageFromResources("MessageSkippingHiddenProperty", name, property.DeclaringType.FullName);
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
                                    Log.LogErrorFromResources("ErrorCannotConvertFromString", name, propertyType.FullName);
                                }
                                else
                                {
                                    Log.LogWarningFromResources("WarningCannotConvertFromString", name, propertyType.FullName);
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
                                        Log.LogErrorFromResources("ErrorConversionFailed", propertyType.FullName, name, String.Join(";", parameterValues.ToArray()));
                                    }
                                    else
                                    {
                                        Log.LogWarningFromResources("WarningConversionFailed", propertyType.FullName, name, String.Join(";", parameterValues.ToArray()));
                                    }
                                }
                            }
                        }
                    }
                    else if (elementType.IsAssignableFrom(typeof(ITaskItem)))
                    {
                        if (isArray)
                        {
                            convertedProperties.Add(name, this.TaskItems ?? new ITaskItem[0]);
                        }
                        else if (this.TaskItems != null && this.TaskItems.Length > 0)
                        {
                            convertedProperties.Add(name, this.TaskItems[0]);
                        }
                        else if (required)
                        {
                            Log.LogErrorFromResources("ErrorMissingRequiredParameter", name);
                        }
                    }
                    else if (isArray)
                    {
                        convertedProperties.Add(name, Array.CreateInstance(elementType, 0));
                        Log.LogMessageFromResources(MessageImportance.Low, "MessageUsingDefaultEmptyArray", name);
                    }
                    else if (required)
                    {
                        Log.LogErrorFromResources("ErrorMissingRequiredParameter", name);
                    }
                }

                if (Log.HasLoggedErrors)
                {
                    return false;
                }

                object task = assembly.CreateInstance(this.TaskNameWithNamespace, true);
                ((ITask)task).BuildEngine = this.BuildEngine;
                ((ITask)task).HostObject = this.HostObject;
                foreach (var kvp in convertedProperties)
                {
                    taskType.GetProperty(kvp.Key, BindingFlags.Instance | BindingFlags.Public).GetSetMethod(false).Invoke(task, new object[] { kvp.Value });
                }
                bool result = ((ITask)task).Execute();
                if (!result)
                {
                    Log.LogErrorFromResources("ErrorInnerTaskFailed", taskType.FullName, assembly.FullName);
                }
                else if (!String.IsNullOrEmpty(this.OutputParameterName))
                {
                    var outputProperty = taskType.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.Name == this.OutputParameterName && p.IsDefined(typeof(OutputAttribute), true) && p.GetAccessors(false).Length == 2);
                    if (outputProperty != null)
                    {
                        var outputValue = outputProperty.GetGetMethod().Invoke(task, null);
                        if (outputValue != null)
                        {
                            if (outputProperty.PropertyType.IsArray)
                            {
                                Type outputElementType = outputProperty.PropertyType.GetElementType();
                                this.InnerTaskOutput = String.Join(";", ((Array)outputValue).OfType<object>().Select(v => Convert.ChangeType(v, outputElementType).ToString()).ToArray());
                            }
                            else
                            {
                                this.InnerTaskOutput = Convert.ChangeType(outputValue, outputProperty.PropertyType).ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// The full name of a strong-named assembly
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// The full path to the custom task DLL
        /// </summary>
        public string AssemblyFile { get; set; }

        /// <summary>
        /// The name of the output parameter from the custom task to copy to
        /// the InnerTaskOutput parameter
        /// </summary>
        public string OutputParameterName { get; set; }

        /// <summary>
        /// The value of the output parameter in the custom task whose name
        /// was specified with OutputParameterName
        /// </summary>
        [Output]
        public string InnerTaskOutput { get; set; }

        /// <summary>
        /// The full name, including the namespace, of the custom task class implementing ITask
        /// </summary>
        [Required]
        public string TaskNameWithNamespace { get; set; }

        /// <summary>
        /// The names of the custom task input parameters -- use the same name
        /// multiple times to specify multiple values for an array
        /// </summary>
        public string[] ParameterNames { get; set; }

        /// <summary>
        /// The values of the custom task input parameters in the same order as specified in
        /// ParameterNames
        /// </summary>
        public string[] ParameterValues { get; set; }

        /// <summary>
        /// An item array that will be passed to any input parameter in the custom task of type
        /// ITaskItem[]
        /// </summary>
        public ITaskItem[] TaskItems { get; set; }
    }
}
