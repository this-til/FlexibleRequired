using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace FlexibleRequired.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiredMemberAnalyzer : DiagnosticAnalyzer {

    public const string DiagnosticId = "RMQ001";

    private const string Category = "Validation";

    private static readonly LocalizableString Title =
        new LocalizableResourceString("AnalyzerTitle", Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString MessageFormat =
        new LocalizableResourceString("AnalyzerMessageFormat", Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString Description =
        new LocalizableResourceString("AnalyzerDescription", Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor
    (
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: null
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context) {
        ObjectCreationExpressionSyntax objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        // 1. 获取创建的对象类型
        INamedTypeSymbol? typeSymbol = context.SemanticModel.GetTypeInfo(objectCreation).Type as INamedTypeSymbol;
        if (typeSymbol == null) {
            return;
        }

        // 2. 获取使用的构造函数及其可选成员信息
        var optionalMembers = GetOptionalMembersFromConstructor(objectCreation, context.SemanticModel, typeSymbol, scanConstructorAssignments: true);

        // 3. 收集所有需要初始化的成员（包括继承的成员）
        List<ISymbol> requiredMembers = GetRequiredMembers(typeSymbol, context.Compilation, optionalMembers).ToList();
        if (!requiredMembers.Any()) {
            return;
        }

        // 4. 检查初始化器中已初始化的成员
        ISet<string>? initializedMembers = objectCreation.Initializer?.ChildNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Select(a => a.Left.ToString())
            .ToImmutableHashSet();

        initializedMembers ??= new HashSet<string>();

        // 5. 收集所有未初始化的成员
        var missingMembers = requiredMembers
            .Where(member => !initializedMembers.Contains(member.Name))
            .ToList();

        if (!missingMembers.Any()) {
            return;
        }

        // 6. 生成单个诊断报告，包含所有缺失的成员
        var missingMemberNames = string.Join(", ", missingMembers.Select(m => m.Name));
        var allMemberNames = string.Join(";", missingMembers.Select(m => m.Name));

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add("MemberNames", allMemberNames);
        properties.Add("TypeName", typeSymbol.Name);

        var diagnostic = Diagnostic.Create
        (
            Rule,
            objectCreation.GetLocation(),
            properties.ToImmutable(),
            missingMemberNames,
            typeSymbol.Name
        );
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// 从构造函数中获取标记为可选的成员
    /// </summary>
    private static HashSet<string> GetOptionalMembersFromConstructor(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, INamedTypeSymbol typeSymbol, bool scanConstructorAssignments = false) {
        var optionalMembers = new HashSet<string>();

        // 获取调用的构造函数
        var constructorSymbol = semanticModel.GetSymbolInfo(objectCreation).Symbol as IMethodSymbol;
        if (constructorSymbol == null) {
            return optionalMembers;
        }

        // 检查构造函数上的 OptionalRequiredAttribute
        var optionalRequiredAttribute = constructorSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name.Contains("OptionalRequired") == true);

        if (optionalRequiredAttribute != null) {
            // 提取可选成员名称
            foreach (var arg in optionalRequiredAttribute.ConstructorArguments) {
                if (arg.Kind == TypedConstantKind.Array) {
                    foreach (var value in arg.Values) {
                        if (value.Value is string memberName) {
                            optionalMembers.Add(memberName);
                        }
                    }
                }
            }
        }

        // 如果启用了构造函数赋值扫描，分析构造函数中已经设置的属性
        if (scanConstructorAssignments) {
            var constructorAssignedMembers = GetConstructorAssignedMembers(constructorSymbol, semanticModel);
            
            // 将构造函数中已经赋值的成员也视为可选（因为它们已经被设置了）
            foreach (var assignedMember in constructorAssignedMembers) {
                optionalMembers.Add(assignedMember);
            }
        }

        return optionalMembers;
    }

    /// <summary>
    /// 获取构造函数中已经赋值的成员
    /// </summary>
    private static HashSet<string> GetConstructorAssignedMembers(IMethodSymbol constructorSymbol, SemanticModel semanticModel) {
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
                else if (constructorSyntax.ExpressionBody != null) {
                    // 处理表达式体构造函数（虽然不常见）
                    // 这里可以根据需要添加更多逻辑
                }
            }
        }

        return assignedMembers;
    }

    private static IEnumerable<ISymbol> GetRequiredMembers(INamedTypeSymbol typeSymbol, Compilation compilation, HashSet<string> optionalMembers) {
        var memberRequirements = new Dictionary<string, (ISymbol Member, bool IsRequired)>();

        // 从当前类型开始，向上遍历继承链
        var currentType = typeSymbol;
        while (currentType != null) {
            foreach (var member in currentType.GetMembers()) {
                if (member is not IFieldSymbol and not IPropertySymbol)
                    continue;

                // 如果这个成员名称还没有被处理过（优先使用最派生类的定义）
                if (!memberRequirements.ContainsKey(member.Name)) {
                    // 检查是否在构造函数的可选成员列表中
                    if (optionalMembers.Contains(member.Name)) {
                        // 如果在可选列表中，则视为 [Required(false)]
                        memberRequirements[member.Name] = (member, false);
                    } else {
                        AttributeData? requiredAttribute = member.GetAttributes()
                            .FirstOrDefault(attr => attr.AttributeClass?.Name.Contains("Required") == true);

                        if (requiredAttribute != null) {
                            // 检查 [Required] 或 [Required(true)]
                            var isRequired = requiredAttribute.ConstructorArguments.IsEmpty ||
                                             (requiredAttribute.ConstructorArguments[0].Value as bool?) != false;

                            memberRequirements[member.Name] = (member, isRequired);
                        }
                    }
                }
            }

            currentType = currentType.BaseType;
        }

        // 只返回标记为必需的成员
        return memberRequirements.Values
            .Where(requirement => requirement.IsRequired)
            .Select(requirement => requirement.Member);
    }

}
