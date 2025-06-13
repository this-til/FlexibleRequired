using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FlexibleRequired.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OptionalMemberValidationAnalyzer : DiagnosticAnalyzer {

    public const string InvalidMemberDiagnosticId = "RMQ002";
    public const string RedundantMemberDiagnosticId = "RMQ003";

    private const string Category = "Validation";

    private static readonly LocalizableString InvalidMemberTitle =
        new LocalizableResourceString("InvalidOptionalMemberTitle", Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString InvalidMemberMessageFormat =
        new LocalizableResourceString("InvalidOptionalMemberMessageFormat", Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString InvalidMemberDescription =
        new LocalizableResourceString("InvalidOptionalMemberDescription", Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString RedundantMemberTitle =
        new LocalizableResourceString("RedundantOptionalMemberTitle", Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString RedundantMemberMessageFormat =
        new LocalizableResourceString("RedundantOptionalMemberMessageFormat", Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString RedundantMemberDescription =
        new LocalizableResourceString("RedundantOptionalMemberDescription", Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor InvalidMemberRule = new DiagnosticDescriptor
    (
        InvalidMemberDiagnosticId,
        InvalidMemberTitle,
        InvalidMemberMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: InvalidMemberDescription,
        helpLinkUri: null
    );

    private static readonly DiagnosticDescriptor RedundantMemberRule = new DiagnosticDescriptor
    (
        RedundantMemberDiagnosticId,
        RedundantMemberTitle,
        RedundantMemberMessageFormat,
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: RedundantMemberDescription,
        helpLinkUri: null
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(InvalidMemberRule, RedundantMemberRule);

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
    }

    private void AnalyzeConstructor(SyntaxNodeAnalysisContext context) {
        var constructorDeclaration = (ConstructorDeclarationSyntax)context.Node;

        // 获取构造函数的符号信息
        var constructorSymbol = context.SemanticModel.GetDeclaredSymbol(constructorDeclaration);
        if (constructorSymbol?.ContainingType == null) {
            return;
        }

        // 查找 OptionalRequiredAttribute
        var optionalRequiredAttribute = constructorSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name.Contains("OptionalRequired") == true);

        if (optionalRequiredAttribute == null) {
            return;
        }

        var containingType = constructorSymbol.ContainingType;

        // 获取类型中所有可访问的成员名称（包括继承的）
        var availableMembers = GetAllAvailableMembers(containingType);

        // 获取构造函数中已经赋值的成员
        var constructorAssignedMembers = GetConstructorAssignedMembers(constructorSymbol);

        // 检查每个指定的可选成员是否存在
        foreach (var arg in optionalRequiredAttribute.ConstructorArguments) {
            if (arg.Kind == TypedConstantKind.Array) {
                foreach (var value in arg.Values) {
                    if (value.Value is string memberName) {
                        // 查找属性中对应的语法节点位置
                        var attributeSyntax = GetOptionalRequiredAttributeSyntax(constructorDeclaration);
                        var location = GetMemberNameLocation(attributeSyntax, memberName) ?? constructorDeclaration.GetLocation();

                        if (!availableMembers.Contains(memberName)) {
                            // 成员不存在
                            var diagnostic = Diagnostic.Create
                            (
                                InvalidMemberRule,
                                location,
                                memberName,
                                containingType.Name
                            );
                            context.ReportDiagnostic(diagnostic);
                        }
                        else if (constructorAssignedMembers.Contains(memberName)) {
                            // 成员已经在构造函数中赋值，不应该在 OptionalRequired 中
                            var diagnostic = Diagnostic.Create
                            (
                                RedundantMemberRule,
                                location,
                                memberName
                            );
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取类型中所有可访问的成员名称（包括继承的属性和字段）
    /// </summary>
    private static ImmutableHashSet<string> GetAllAvailableMembers(INamedTypeSymbol typeSymbol) {
        var members = ImmutableHashSet.CreateBuilder<string>();

        var currentType = typeSymbol;
        while (currentType != null) {
            foreach (var member in currentType.GetMembers()) {
                if (member is IFieldSymbol or IPropertySymbol) {
                    // 只包含可以被初始化的成员
                    if (member.DeclaredAccessibility != Accessibility.Private || 
                        member.ContainingType.Equals(typeSymbol, SymbolEqualityComparer.Default)) {
                        members.Add(member.Name);
                    }
                }
            }

            currentType = currentType.BaseType;
        }

        return members.ToImmutable();
    }

    /// <summary>
    /// 获取 OptionalRequiredAttribute 的语法节点
    /// </summary>
    private static AttributeSyntax? GetOptionalRequiredAttributeSyntax(ConstructorDeclarationSyntax constructorDeclaration) {
        return constructorDeclaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(attr => attr.Name.ToString().Contains("OptionalRequired"));
    }

    /// <summary>
    /// 尝试获取特定成员名称在属性参数中的位置
    /// </summary>
    private static Location? GetMemberNameLocation(AttributeSyntax? attributeSyntax, string memberName) {
        if (attributeSyntax?.ArgumentList?.Arguments == null) {
            return null;
        }

        foreach (var argument in attributeSyntax.ArgumentList.Arguments) {
            if (argument.Expression is LiteralExpressionSyntax literal &&
                literal.Token.ValueText == memberName) {
                return literal.GetLocation();
            }
        }

        // 如果找不到具体位置，返回整个属性的位置
        return attributeSyntax.GetLocation();
    }

    /// <summary>
    /// 获取构造函数中已经赋值的成员
    /// </summary>
    private static HashSet<string> GetConstructorAssignedMembers(IMethodSymbol constructorSymbol) {
        var assignedMembers = new HashSet<string>();

        // 获取构造函数的语法声明
        var constructorDeclarations = constructorSymbol.DeclaringSyntaxReferences;
        
        foreach (var syntaxRef in constructorDeclarations) {
            if (syntaxRef.GetSyntax() is ConstructorDeclarationSyntax constructorSyntax) {
                if (constructorSyntax.Body != null) {
                    // 扫描构造函数体中的赋值语句
                    var assignments = constructorSyntax.Body.DescendantNodes()
                        .OfType<AssignmentExpressionSyntax>()
                        .Where(assignment => assignment.IsKind(SyntaxKind.SimpleAssignmentExpression));

                    foreach (var assignment in assignments) {
                        // 检查赋值的左侧是否是属性或字段访问
                        if (assignment.Left is IdentifierNameSyntax identifier) {
                            // 直接属性赋值：PropertyName = value
                            assignedMembers.Add(identifier.Identifier.ValueText);
                        }
                        else if (assignment.Left is MemberAccessExpressionSyntax memberAccess) {
                            // 成员访问赋值：this.PropertyName = value 或 object.PropertyName = value
                            if (memberAccess.Expression is ThisExpressionSyntax || 
                                (memberAccess.Expression is IdentifierNameSyntax && 
                                 memberAccess.Expression.ToString() == "this")) {
                                assignedMembers.Add(memberAccess.Name.Identifier.ValueText);
                            }
                        }
                    }
                }
            }
        }

        return assignedMembers;
    }
} 