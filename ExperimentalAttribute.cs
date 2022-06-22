using System;
using System.Diagnostics;

namespace JA
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute()
        {
            Debug.WriteLine("Experimental Feature Used.");
        }
    }
}
