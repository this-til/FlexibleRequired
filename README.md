# FlexibleRequired 示例

本项目展示了 FlexibleRequired 分析器和特性系统的各种使用场景。

## 运行示例

```bash
cd FlexibleRequired.Sample
dotnet run
```

## 示例分类

### 1. 基础用法示例
- **Person 类**: 展示属性上的基本 `[Required]` 特性
- 演示带有必需属性的有效对象初始化
- 展示可选属性可以被省略

### 2. 继承示例
- **Employee → Manager**: 展示派生类如何将可选属性变为必需
- **Employee → Contractor**: 展示派生类如何使用 `[Required(false)]` 放宽要求
- 演示具有不同要求级别的继承层次结构

### 3. 不同数据类型示例
- **Product 类**: 展示各种数据类型上的 `[Required]` 特性：
  - 基本类型 (`string`, `decimal`)
  - 集合类型 (`List<string>`, `Dictionary<string, string>`)
  - 带有默认值的可选属性

### 4. 真实世界场景
- **ApiRequest**: 带有一些自动生成属性的 API 请求模型
- **DatabaseConfig**: 带有必需和可选设置的配置对象
- **UserPreferences → AdminPreferences**: 基于角色要求的用户设置

### 5. OptionalRequired 构造函数示例
- **User 类**: 展示构造函数上的 `[OptionalRequired]` 特性来有选择地使属性变为可选
- **ServerConfig 类**: 演示具有不同可选成员配置的多个构造函数
- 展示构造函数级别的特性如何覆盖属性级别的 `[Required]` 特性

## 主要功能演示

### 必需特性
```csharp
[Required]
public string Name { get; init; } = "";
```

### 可选属性
```csharp
// 没有 [Required] 特性意味着可选
public int? Age { get; init; }
```

### 继承覆盖
```csharp
// 在派生类中使属性变为可选
[Required(false)]
public override string EmployeeId { get; init; } = "";

// 在派生类中使可选属性变为必需
[Required]
public new decimal? Salary { get; init; }
```

### 默认值
```csharp
// 带有默认值的属性
public bool InStock { get; init; } = true;
public DateTime Timestamp { get; init; } = DateTime.UtcNow;
```

### OptionalRequired 构造函数
```csharp
public class User
{
    [Required]
    public string Username { get; init; } = "";
    
    [Required]
    public string Email { get; init; } = "";
    
    [Required]
    public string DisplayName { get; init; } = "";
    
    [Required]
    public string Bio { get; init; } = "";

    // 使 DisplayName 和 Bio 变为可选的构造函数
    [OptionalRequired("DisplayName", "Bio")]
    public User(string username, string email)
    {
        Username = username;
        Email = email;
        DisplayName = username; // 默认值
        Bio = ""; // 默认值
    }

    // 具有不同可选要求的构造函数
    [OptionalRequired("DisplayName")]
    public User(string username, string email, bool isPremium)
    {
        Username = username;
        Email = email;
        DisplayName = username;
        IsPremium = isPremium;
        // 使用此构造函数时 Bio 仍然是必需的
    }
}
```

## 错误示例（已注释）

文件中包含已注释的错误示例，当 FlexibleRequired 分析器激活时会触发编译错误：

- 缺少必需属性
- 不完整的对象初始化
- 继承要求违规

要查看这些错误的实际效果，请取消注释错误示例部分并尝试构建项目。

## 分析器行为

当启用 FlexibleRequired 分析器时，它将：

1. **分析对象初始化器** 以确保所有 `[Required]` 属性都已设置
2. **遵循继承层次结构** 和属性覆盖
3. **处理 OptionalRequired 构造函数** - 当使用带有 `[OptionalRequired]` 的构造函数时，指定的成员对该初始化变为可选
4. **验证 OptionalRequired 特性** - 确保 `[OptionalRequired]` 中指定的成员名称确实存在于类中
5. **提供有用的错误消息** 指向缺少的必需属性
6. **支持复杂场景** 如集合、泛型和嵌套对象

### OptionalRequired 验证

分析器包含三个诊断规则：

- **RMQ001**: 对象初始化期间缺少必需成员
- **RMQ002**: `[OptionalRequired]` 特性中的无效成员名称（当指定的成员不存在时发出警告）
- **RMQ003**: `[OptionalRequired]` 特性中的冗余成员（当成员已在构造函数中赋值时发出提示）

## 构建和测试

示例项目引用：
- `FlexibleRequired`（特性库）
- `FlexibleRequired.Analyzers`（Roslyn 分析器）

当你构建此项目时，分析器将自动验证你的对象初始化并将任何缺少的必需属性报告为编译错误。 