﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IsolatedTask {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class LogText {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal LogText() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("IsolatedTask.LogText", typeof(LogText).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to assembly.
        /// </summary>
        internal static string Assembly {
            get {
                return ResourceManager.GetString("Assembly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Loading assembly with name &apos;{0}&apos; into the AppDomain &apos;{1}&apos; with BaseDirectory &apos;{2}&apos; failed.  See subsequent exception..
        /// </summary>
        internal static string ErrorAssemblyLoadFailed {
            get {
                return ResourceManager.GetString("ErrorAssemblyLoadFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Either AssemblyName or AssemblyFile must be set..
        /// </summary>
        internal static string ErrorAssemblyNameOrFileMustBeSet {
            get {
                return ResourceManager.GetString("ErrorAssemblyNameOrFileMustBeSet", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter &apos;{0}&apos; is of type &apos;{1}&apos;, which cannot be converted from string (or its elements cannot).  The parameter is required, so this is an error..
        /// </summary>
        internal static string ErrorCannotConvertFromString {
            get {
                return ResourceManager.GetString("ErrorCannotConvertFromString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Conversion from string to {0} failed for parameter &apos;{1}&apos;.  Property value was &apos;{2}&apos;.  The parameter is required, so this is an error..
        /// </summary>
        internal static string ErrorConversionFailed {
            get {
                return ResourceManager.GetString("ErrorConversionFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Inner task &apos;{0}&apos; in assembly &apos;{1}&apos; failed.  There are probably other error messages..
        /// </summary>
        internal static string ErrorInnerTaskFailed {
            get {
                return ResourceManager.GetString("ErrorInnerTaskFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The parameter &apos;{0}&apos; is not of an array type, and it is marked with RequiredAttribute, but the parameter is not specified..
        /// </summary>
        internal static string ErrorMissingRequiredParameter {
            get {
                return ResourceManager.GetString("ErrorMissingRequiredParameter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Either AssemblyName or AssemblyFile must be set, but not both..
        /// </summary>
        internal static string ErrorOnlyOneCanBeSet {
            get {
                return ResourceManager.GetString("ErrorOnlyOneCanBeSet", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ParameterNames has {0} element(s), but ParameterValues has {1} element(s).  The two arrays must be the same length..
        /// </summary>
        internal static string ErrorParameterNamesAndValuesMustHaveSameLength {
            get {
                return ResourceManager.GetString("ErrorParameterNamesAndValuesMustHaveSameLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Type &apos;{0}&apos; in assembly &apos;{1}&apos; is an abstract type, not a concrete type..
        /// </summary>
        internal static string ErrorTypeIsAbstract {
            get {
                return ResourceManager.GetString("ErrorTypeIsAbstract", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Type &apos;{0}&apos; in assembly &apos;{1}&apos; is an interface, not a concrete type..
        /// </summary>
        internal static string ErrorTypeIsInterface {
            get {
                return ResourceManager.GetString("ErrorTypeIsInterface", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Type &apos;{0}&apos; was not found in assembly &apos;{1}&apos;..
        /// </summary>
        internal static string ErrorTypeNotFound {
            get {
                return ResourceManager.GetString("ErrorTypeNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Type &apos;{0}&apos; in assembly &apos;{1}&apos; does not implement Microsoft.Build.Framework.ITask from assembly &apos;{2}&apos;..
        /// </summary>
        internal static string ErrorTypeNotITask {
            get {
                return ResourceManager.GetString("ErrorTypeNotITask", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to resolve {0} &apos;{1}&apos; from within {2}..
        /// </summary>
        internal static string ErrorUnableToResolveWithin {
            get {
                return ResourceManager.GetString("ErrorUnableToResolveWithin", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Assembly with name &apos;{0}&apos; loaded from file &apos;{1}&apos;.
        /// </summary>
        internal static string MessageAssemblyLoadedFromFile {
            get {
                return ResourceManager.GetString("MessageAssemblyLoadedFromFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Assembly loaded by name: &apos;{0}&apos;.
        /// </summary>
        internal static string MessageAssemblyLoadedFromName {
            get {
                return ResourceManager.GetString("MessageAssemblyLoadedFromName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Skipping parameter &apos;{0}&apos; declared in type &apos;{1}&apos; because it is hidden or overridden by a property with the same name in a more derived type..
        /// </summary>
        internal static string MessageSkippingHiddenProperty {
            get {
                return ResourceManager.GetString("MessageSkippingHiddenProperty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter &apos;{0}&apos; is an array type, but no value was specified.  Initializing with an empty array..
        /// </summary>
        internal static string MessageUsingDefaultEmptyArray {
            get {
                return ResourceManager.GetString("MessageUsingDefaultEmptyArray", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to type.
        /// </summary>
        internal static string Type {
            get {
                return ResourceManager.GetString("Type", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter &apos;{0}&apos; is of type &apos;{1}&apos;, which cannot be converted from string (or its elements cannot).  Skipping parameter..
        /// </summary>
        internal static string WarningCannotConvertFromString {
            get {
                return ResourceManager.GetString("WarningCannotConvertFromString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Conversion from string to {0} failed for parameter &apos;{1}&apos;.  Property value was &apos;{2}&apos;.  Skipping parameter..
        /// </summary>
        internal static string WarningConversionFailed {
            get {
                return ResourceManager.GetString("WarningConversionFailed", resourceCulture);
            }
        }
    }
}
