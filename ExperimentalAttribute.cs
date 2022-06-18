using System;
using System.Diagnostics;

namespace JA
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute()
        {
            Debug.WriteLine("Experimental Feature Used.");
        }
    }
}
