using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.IO;
using System.Reflection;
using SR = System.Reflection;
using System.ComponentModel;

namespace IsolatedTask
{
    [Serializable]
    public class IsolatedTask : AppDomainIsolatedTask
    {
        private bool m_inLoadEvent;

        public IsolatedTask()
            : base(LogText.ResourceManager)
        {
        }

        public override bool Execute()
        {
            m_inLoadEvent = false;

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

            AppDomain appDomainToUse = AppDomain.CurrentDomain;
            try
            {
                // Determine whether we can or need to load the assembly into a separate AppDomain.
                // If the assembly is specified by file, and the type we need derives from MarshalByRefObject,
                // we almost certainly need to do so.  Otherwise, the assembly loads fine (in .NET 4.0) into
                // load-from context, but the assembly.GetType call fails.
                m_determinedAssemblyName = String.IsNullOrEmpty(this.AssemblyName) ? SR.AssemblyName.GetAssemblyName(this.AssemblyFile) : new AssemblyName(this.AssemblyName);
                if (!String.IsNullOrEmpty(this.AssemblyFile))
                {
                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                    AppDomain.CurrentDomain.TypeResolve += CurrentDomain_TypeResolve;
                    string applicationBase = Path.GetDirectoryName(this.AssemblyFile);
                    var appDomainSetup = new AppDomainSetup()
                    {
                        ApplicationBase = applicationBase
                    };
                    appDomainToUse = AppDomain.CreateDomain("IsolatedTaskAdditionalDomain", null, appDomainSetup);
                }

                var thisObjRef = this.CreateObjRef(this.GetType());
                var remoteTaskPortion = new RemoteTaskPortion(thisObjRef);
                appDomainToUse.DoCallBack(new CrossAppDomainDelegate(remoteTaskPortion.Execute));
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
            }
            finally
            {
                if (!Object.ReferenceEquals(appDomainToUse, AppDomain.CurrentDomain))
                {
                    AppDomain.Unload(appDomainToUse);
                }
                if (!String.IsNullOrEmpty(this.AssemblyFile))
                {
                    AppDomain.CurrentDomain.TypeResolve -= CurrentDomain_TypeResolve;
                    AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                }
            }

            return !Log.HasLoggedErrors;
        }

        private Assembly CurrentDomain_TypeResolve(object sender, ResolveEventArgs args)
        {
            if (m_inLoadEvent)
            {
                return null;
            }

            try
            {
                m_inLoadEvent = true;
                if (args.Name.Equals(this.TaskNameWithNamespace, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (String.IsNullOrEmpty(this.AssemblyFile))
                    {
                        return Assembly.Load(m_determinedAssemblyName);
                    }
                    return Assembly.LoadFrom(this.AssemblyFile);
                }
                Log.LogErrorFromResources("ErrorUnableToResolveWithin", LogText.Type, args.Name, "IsolatedTask.CurrentDomain_TypeResolve");
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
                return null;
            }

            try
            {
                m_inLoadEvent = true;
                var requestedName = new SR.AssemblyName(args.Name);
                if (requestedName.Name.Equals(m_determinedAssemblyName.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (String.IsNullOrEmpty(this.AssemblyFile))
                    {
                        return Assembly.Load(m_determinedAssemblyName);
                    }
                    return Assembly.LoadFrom(this.AssemblyFile);
                }

                Log.LogErrorFromResources("ErrorUnableToResolveWithin", LogText.Assembly, args.Name, "IsolatedTask.CurrentDomain_AssemblyResolve");
                return null;
            }
            finally
            {
                m_inLoadEvent = false;
            }
        }

        private SR.AssemblyName m_determinedAssemblyName = null;
        internal SR.AssemblyName DeterminedAssemblyName
        {
            get
            {
                return m_determinedAssemblyName;
            }
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
