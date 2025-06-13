// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;

namespace FlexibleRequired.Sample;

public class Examples {

    public static void Main() {
        Console.WriteLine("=== FlexibleRequired Examples ===\n");

        // Basic Usage Examples
        BasicUsageExamples();

        // Inheritance Examples
        InheritanceExamples();

        // Different Data Types Examples
        DataTypeExamples();

        // Real-world Scenarios
        RealWorldExamples();

        // OptionalRequired Constructor Examples
        OptionalRequiredExamples();

        // Error Examples (commented out as they would cause compilation errors)
        // ErrorExamples();
    }

    private static void BasicUsageExamples() {
        Console.WriteLine("1. Basic Usage Examples:");

        // Valid: All required properties are set
        var validPerson = new Person {
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30 // Optional property
        };
        Console.WriteLine($"✅ Valid Person: {validPerson.Name}, {validPerson.Email}");

        // Valid: Only required properties are set
        var minimalPerson = new Person {
            Name = "Jane Doe",
            Email = "jane@example.com"
            // Age is optional, so we can omit it
        };
        Console.WriteLine($"✅ Minimal Person: {minimalPerson.Name}, {minimalPerson.Email}");

        Console.WriteLine();
    }

    private static void InheritanceExamples() {
        Console.WriteLine("2. Inheritance Examples:");

        // Base class with required properties
        var employee = new Employee {
            Name = "Alice Smith",
            Email = "alice@company.com",
            EmployeeId = "EMP001",
            Department = "Engineering"
            // Salary is optional in Employee
        };
        Console.WriteLine($"✅ Employee: {employee.Name} ({employee.EmployeeId})");

        // Derived class that makes optional property required
        var manager = new Manager {
            Name = "Bob Johnson",
            Email = "bob@company.com",
            EmployeeId = "MGR001",
            Department = "Engineering",
            Salary = 90000, // Required in Manager (was optional in Employee)
            TeamSize = 5
        };
        Console.WriteLine($"✅ Manager: {manager.Name}, Salary: {manager.Salary}, Team: {manager.TeamSize}");

        // Contractor with relaxed requirements
        var contractor = new Contractor {
            Name = "Charlie Brown",
            Email = "charlie@contractor.com"
            // EmployeeId not required for contractors
            // Department not required for contractors
        };
        Console.WriteLine($"✅ Contractor: {contractor.Name}");

        Console.WriteLine();
    }

    private static void DataTypeExamples() {
        Console.WriteLine("3. Different Data Types Examples:");

        var product = new Product {
            Name = "Laptop",
            Price = 999.99m,
            Category = "Electronics",
            Tags = new List<string> { "Computer", "Portable" },
            Specifications = new Dictionary<string, string> {
                ["CPU"] = "Intel i7",
                ["RAM"] = "16GB"
            }
            // Description is optional
            // InStock has default value
        };
        Console.WriteLine($"✅ Product: {product.Name} - ${product.Price}");
        Console.WriteLine($"   Tags: {string.Join(", ", product.Tags)}");

        Console.WriteLine();
    }

    private static void RealWorldExamples() {
        Console.WriteLine("4. Real-world Scenarios:");

        // API Request/Response models
        var apiRequest = new ApiRequest {
            UserId = "user123",
            Action = "UPDATE_PROFILE",
            Data = new { name = "New Name", email = "new@email.com" }
            // Timestamp will be set automatically
            // RequestId is optional
        };
        Console.WriteLine($"✅ API Request: {apiRequest.Action} for {apiRequest.UserId}");

        // Configuration objects
        var dbConfig = new DatabaseConfig {
            ConnectionString = "Server=localhost;Database=MyDb",
            Provider = "SqlServer"
            // Timeout has default value
            // PoolSize has default value
        };
        Console.WriteLine($"✅ Database Config: {dbConfig.Provider}");

        // User preferences with inheritance
        var adminPrefs = new AdminPreferences {
            UserId = "admin123",
            Theme = "Dark",
            Language = "en-US",
            CanModerateUsers = true,
            CanDeletePosts = true
            // NotificationsEnabled has default value
        };
        Console.WriteLine($"✅ Admin Preferences: {adminPrefs.UserId}, Theme: {adminPrefs.Theme}");

        Console.WriteLine();
    }

    private static void OptionalRequiredExamples() {
        Console.WriteLine("5. OptionalRequired Constructor Examples:");

        // Using constructor with OptionalRequired attribute
        var basicUser = new User("john_doe", "john@example.com");
        Console.WriteLine($"✅ Basic User: {basicUser.Username}, {basicUser.Email}");
        // DisplayName and Bio are optional due to OptionalRequired attribute

        // Using different constructor with different optional members
        var premiumUser = new User("jane_premium", "jane@example.com", isPremium: true);
        Console.WriteLine($"✅ Premium User: {premiumUser.Username}, Premium: {premiumUser.IsPremium}");
        // For premium users, DisplayName is still optional but Bio is required

        // Complex configuration example
        var serverConfig = new ServerConfig("localhost", 8080);
        Console.WriteLine($"✅ Server Config: {serverConfig.Host}:{serverConfig.Port}");
        // Using constructor that makes SSL and database settings optional

        var productionConfig = new ServerConfig("prod.example.com", 443, true);
        Console.WriteLine($"✅ Production Config: {productionConfig.Host}:{productionConfig.Port}, SSL: {productionConfig.EnableSsl}");
        // Using constructor that requires SSL but makes database settings optional

        Console.WriteLine();
    }

}

// Basic example classes
public class Person {

    [Required]
    public string Name { get; init; } = "";

    [Required]
    public string Email { get; init; } = "";

    // Optional property
    public int? Age { get; init; }

}

// Inheritance examples
public class Employee : Person {

    [Required]
    public string EmployeeId { get; init; } = "";

    [Required]
    public string Department { get; init; } = "";

    // Optional in base Employee class
    public decimal? Salary { get; init; }

}

public class Manager : Employee {

    // Make salary required for managers
    [Required]
    public new decimal? Salary { get; init; }

    [Required]
    public int TeamSize { get; init; }

}

public class Contractor : Person {

    // Contractors don't need employee ID or department
    [Required(false)]
    public new string EmployeeId { get; init; } = "";

    [Required(false)]
    public new string Department { get; init; } = "";

    public string? ContractorId { get; init; }

}

// Different data types
public class Product {

    [Required]
    public string Name { get; init; } = "";

    [Required]
    public decimal Price { get; init; }

    [Required]
    public string Category { get; init; } = "";

    [Required]
    public List<string> Tags { get; init; } = new();

    [Required]
    public Dictionary<string, string> Specifications { get; init; } = new();

    // Optional properties
    public string? Description { get; init; }

    public bool InStock { get; init; } = true;

}

// Real-world scenarios
public class ApiRequest {

    [Required]
    public string UserId { get; init; } = "";

    [Required]
    public string Action { get; init; } = "";

    [Required]
    public object Data { get; init; } = new();

    // Auto-generated, not required in initialization
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    // Optional
    public string? RequestId { get; init; }

}

public class DatabaseConfig {

    [Required]
    public string ConnectionString { get; init; } = "";

    [Required]
    public string Provider { get; init; } = "";

    // Optional with defaults
    public int Timeout { get; init; } = 30;

    public int PoolSize { get; init; } = 10;

}

public class UserPreferences {

    [Required]
    public string UserId { get; init; } = "";

    [Required]
    public string Theme { get; init; } = "";

    [Required]
    public string Language { get; init; } = "";

    public bool NotificationsEnabled { get; init; } = true;

}

public class AdminPreferences : UserPreferences {

    [Required]
    public bool CanModerateUsers { get; init; }

    [Required]
    public bool CanDeletePosts { get; init; }

}

// OptionalRequired attribute examples
public class User {

    [Required]
    public string Username { get; init; } = "";

    [Required]
    public string Email { get; init; } = "";

    [Required]
    public string DisplayName { get; init; } = "";

    [Required]
    public string Bio { get; init; } = "";

    public bool IsPremium { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Basic constructor - makes DisplayName and Bio optional
    [OptionalRequired()]
    public User(string username, string email) {
        Username = username;
        Email = email;
        DisplayName = username; // Use username as default display name
        Bio = ""; // Default empty bio
    }

    // Premium constructor - makes DisplayName optional but Bio is required
    [OptionalRequired()]
    public User(string username, string email, bool isPremium) {
        Username = username;
        Email = email;
        DisplayName = username;
        IsPremium = isPremium;
        Bio = isPremium
            ? "Premium user"
            : "";
    }

    // Full constructor - all properties must be set explicitly
    public User() {
        // Object initializer syntax will require all [Required] properties
    }

}

public class ServerConfig {

    [Required]
    public string Host { get; init; } = "";

    [Required]
    public int Port { get; init; }

    [Required]
    public bool EnableSsl { get; init; }

    [Required]
    public string DatabaseConnectionString { get; init; } = "";

    [Required]
    public int MaxConnections { get; init; }

    public string? LogLevel { get; init; } = "Info";

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    // Basic constructor - SSL and database settings are optional
    [OptionalRequired("Host", "Port", "EnableSsl", "DatabaseConnectionString", "MaxConnections")]
    public ServerConfig(string host, int port) {
        Host = host;
        Port = port;
        EnableSsl = false; // Default to non-SSL
        DatabaseConnectionString = ""; // No database by default
        MaxConnections = 100; // Default connection limit
    }

    // Secure constructor - database settings are optional but SSL is required
    [OptionalRequired("Host", "Port", "DatabaseConnectionString", "MaxConnections")]
    public ServerConfig(string host, int port, bool enableSsl) {
        Host = host;
        Port = port;
        EnableSsl = enableSsl;
        DatabaseConnectionString = ""; // No database by default
        MaxConnections = 100; // Default connection limit
    }

    // Full constructor - requires explicit initialization of all properties
    public ServerConfig() {
        // Object initializer syntax will require all [Required] properties
    }

}

/*
// Error Examples (These would cause compilation errors when FlexibleRequired analyzer is active)

public class ErrorExamples
{
    public static void DemonstrateErrors()
    {
        // ❌ Error: Missing required property 'Email'
        var invalidPerson1 = new Person
        {
            Name = "John Doe"
            // Email is missing - this should cause an analyzer error
        };

        // ❌ Error: Missing required property 'Name'
        var invalidPerson2 = new Person
        {
            Email = "john@example.com"
            // Name is missing - this should cause an analyzer error
        };

        // ❌ Error: Missing required property 'Salary' for Manager
        var invalidManager = new Manager
        {
            Name = "Bob Johnson",
            Email = "bob@company.com",
            EmployeeId = "MGR001",
            Department = "Engineering",
            TeamSize = 5
            // Salary is missing - required for Manager even though optional for Employee
        };

        // ❌ Error: Missing multiple required properties
        var invalidProduct = new Product
        {
            Name = "Laptop"
            // Missing Price, Category, Tags, Specifications - all required
        };

        // ❌ Error: Missing required property even when using OptionalRequired constructor
        var invalidUser = new User("john_doe", "jane@example.com", isPremium: true);
        // This constructor makes DisplayName optional but Bio is still required
        // Missing Bio property would cause an error

        // ❌ Error: Invalid member name in OptionalRequired attribute
        // This would cause RMQ002 warning during compilation:
        public class InvalidExample
        {
            [Required]
            public string Name { get; init; } = "";

            [OptionalRequired("NonExistentProperty")] // ❌ This member doesn't exist
            public InvalidExample(string name)
            {
                Name = name;
            }
        }

    }
}
*/
