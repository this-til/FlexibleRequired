using System;

namespace FlexibleRequired;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class RequiredAttribute : Attribute {

    public bool IsRequired { get; }

    public RequiredAttribute() : this(true) { }

    public RequiredAttribute(bool isRequired) {
        IsRequired = isRequired;
    }

}
