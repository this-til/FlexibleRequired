using System;

/// <summary>
/// 标记在构造方法上的注解，用于指定一些字段为可选的（等同于 [Required(false)]）
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
public sealed class OptionalRequiredAttribute : Attribute {

    public string[] OptionalMembers { get; }

    /// <summary>
    /// 构造函数，指定哪些成员应该被视为可选的
    /// </summary>
    /// <param name="optionalMembers">可选成员的名称数组</param>
    public OptionalRequiredAttribute(params string[] optionalMembers) {
        OptionalMembers = optionalMembers ?? throw new ArgumentNullException(nameof(optionalMembers));
    }

}
