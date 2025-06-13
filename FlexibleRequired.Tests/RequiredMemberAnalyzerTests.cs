using System.Threading.Tasks;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
        FlexibleRequired.Analyzers.RequiredMemberAnalyzer>;

namespace FlexibleRequired.Tests;

public class RequiredMemberAnalyzerTests {

    [Fact]
    public async Task ObjectCreation_WithMissingRequiredMembers_ShouldReportDiagnostic() {
        const string text = @"
using FlexibleRequired;

public class TestClass {
    [Required]
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
    
    public void Test() {
        var obj = new TestClass {
            Name = ""Test""
            // Age missing - should trigger diagnostic
        };
    }
}
";

        var expected = Verifier.Diagnostic()
            .WithLocation(12, 19)
            .WithArguments("Age", "TestClass");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }

    [Fact]
    public async Task ObjectCreation_WithAllRequiredMembers_ShouldNotReportDiagnostic() {
        const string text = @"
using FlexibleRequired;

public class TestClass {
    [Required]
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
    
    public void Test() {
        var obj = new TestClass {
            Name = ""Test"",
            Age = 25
        };
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task ObjectCreation_WithOptionalRequiredAttribute_ShouldNotRequireOptionalMembers() {
        const string text = @"
using FlexibleRequired;

public class Person {
    [Required]
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
    
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string Phone { get; set; }

    public Person() { }

    [OptionalRequired(""Email"", ""Phone"")]
    public Person(string name, int age) {
        Name = name;
        Age = age;
    }
}

public class TestClass {
    public void Test() {
        // Using constructor with OptionalRequired - Email and Phone should not be required
        var person = new Person(""John"", 30) {
            // Email and Phone are optional, only need to initialize if desired
        };
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task ObjectCreation_WithOptionalRequiredAttribute_ShouldStillRequireNonOptionalMembers() {
        const string text = @"
using FlexibleRequired;

public class Person {
    [Required]
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
    
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string Phone { get; set; }
    
    [Required]
    public string Address { get; set; }

    public Person() { }

    [OptionalRequired(""Email"", ""Phone"")]
    public Person(string name, int age) {
        Name = name;
        Age = age;
    }
}

public class TestClass {
    public void Test() {
        // Using constructor with OptionalRequired - Address should still be required
        var person = new Person(""John"", 30) {
            // Address missing - should trigger diagnostic
        };
    }
}
";

        var expected = Verifier.Diagnostic()
            .WithLocation(32, 23)
            .WithArguments("Address", "Person");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }

    [Fact]
    public async Task ObjectCreation_WithDefaultConstructor_ShouldRequireAllMembers() {
        const string text = @"
using FlexibleRequired;

public class Person {
    [Required]
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
    
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string Phone { get; set; }

    public Person() { }

    [OptionalRequired(""Email"", ""Phone"")]
    public Person(string name, int age) {
        Name = name;
        Age = age;
    }
}

public class TestClass {
    public void Test() {
        // Using default constructor - all Required members should be required
        var person = new Person {
            Name = ""John"",
            Age = 30
            // Email and Phone missing - should trigger diagnostic
        };
    }
}
";

        var expected = Verifier.Diagnostic()
            .WithLocation(28, 23)
            .WithArguments("Email, Phone", "Person");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }

    [Fact]
    public async Task ObjectCreation_WithRequiredFalse_ShouldNotRequireMembers() {
        const string text = @"
using FlexibleRequired;

public class TestClass {
    [Required]
    public string Name { get; set; }
    
    [Required(false)]
    public int Age { get; set; }
    
    public void Test() {
        var obj = new TestClass {
            Name = ""Test""
            // Age with [Required(false)] should not be required
        };
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task ObjectCreation_WithInheritance_ShouldCheckBaseClassMembers() {
        const string text = @"
using FlexibleRequired;

public class BaseClass {
    [Required]
    public string BaseName { get; set; }
}

public class DerivedClass : BaseClass {
    [Required]
    public string DerivedName { get; set; }
    
    public void Test() {
        var obj = new DerivedClass {
            DerivedName = ""Test""
            // BaseName missing - should trigger diagnostic
        };
    }
}
";

        var expected = Verifier.Diagnostic()
            .WithLocation(15, 19)
            .WithArguments("BaseName", "DerivedClass");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
} 