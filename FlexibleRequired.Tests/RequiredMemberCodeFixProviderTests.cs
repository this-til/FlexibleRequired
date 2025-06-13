using System.Threading.Tasks;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
        FlexibleRequired.Analyzers.RequiredMemberAnalyzer,
        FlexibleRequired.Analyzers.RequiredMemberCodeFixProvider>;

namespace FlexibleRequired.Tests;

public class RequiredMemberCodeFixProviderTests {

    [Fact]
    public async Task ObjectCreation_WithMissingRequiredMember_ShouldProvideCodeFix() {
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
        };
    }
}
";

        const string fixedText = @"
using FlexibleRequired;

public class TestClass {
    [Required]
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
    
    public void Test() {
        var obj = new TestClass {
            Name = ""Test"",
            Age = 0
        };
    }
}
";

        var expected = Verifier.Diagnostic()
            .WithLocation(12, 19)
            .WithArguments("Age", "TestClass");
        await Verifier.VerifyCodeFixAsync(text, expected, fixedText);
    }

    [Fact]
    public async Task ObjectCreation_WithMultipleMissingMembers_ShouldProvideCodeFix() {
        const string text = @"
using FlexibleRequired;

public class TestClass {
    [Required]
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
    
    [Required]
    public string Email { get; set; }
    
    public void Test() {
        var obj = new TestClass {
            Name = ""Test""
        };
    }
}
";

        const string fixedText = @"
using FlexibleRequired;

public class TestClass {
    [Required]
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
    
    [Required]
    public string Email { get; set; }
    
    public void Test() {
        var obj = new TestClass {
            Name = ""Test"",
            Age = 0,
            Email = """"
        };
    }
}
";

        var expected = Verifier.Diagnostic()
            .WithLocation(15, 19)
            .WithArguments("Age, Email", "TestClass");
        await Verifier.VerifyCodeFixAsync(text, expected, fixedText);
    }

    [Fact]
    public async Task ObjectCreation_WithNoInitializer_ShouldProvideCodeFix() {
        const string text = @"
using FlexibleRequired;

public class TestClass {
    [Required]
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
    
    public void Test() {
        var obj = new TestClass();
    }
}
";

        const string fixedText = @"
using FlexibleRequired;

public class TestClass {
    [Required]
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
    
    public void Test() {
        var obj = new TestClass()
        {
            Name = """",
            Age = 0
        };
    }
}
";

        var expected = Verifier.Diagnostic()
            .WithLocation(12, 19)
            .WithArguments("Name, Age", "TestClass");
        await Verifier.VerifyCodeFixAsync(text, expected, fixedText);
    }

    [Fact]
    public async Task ObjectCreation_WithOptionalRequiredAttribute_ShouldOnlyFixNonOptionalMembers() {
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
        var person = new Person(""John"", 30) {
        };
    }
}
";

        const string fixedText = @"
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
        var person = new Person(""John"", 30) {
            Address = """"
        };
    }
}
";

        var expected = Verifier.Diagnostic()
            .WithLocation(28, 23)
            .WithArguments("Address", "Person");
        await Verifier.VerifyCodeFixAsync(text, expected, fixedText);
    }
} 