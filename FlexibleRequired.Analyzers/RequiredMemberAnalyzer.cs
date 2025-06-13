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

    public const string DiagnosticId = "REQ001";

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

        // 2. 收集所有需要初始化的成员（包括继承的成员）
        List<ISymbol> requiredMembers = GetRequiredMembers(typeSymbol, context.Compilation).ToList();
        if (!requiredMembers.Any()) {
            return;
        }

        // 3. 检查初始化器中已初始化的成员
        ISet<string>? initializedMembers = objectCreation.Initializer?.ChildNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Select(a => a.Left.ToString())
            .ToImmutableHashSet();

        initializedMembers ??= new HashSet<string>();

        // 4. 收集所有未初始化的成员
        var missingMembers = requiredMembers
            .Where(member => !initializedMembers.Contains(member.Name))
            .ToList();

        if (!missingMembers.Any()) {
            return;
        }

        // 5. 生成单个诊断报告，包含所有缺失的成员
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

    private static IEnumerable<ISymbol> GetRequiredMembers(INamedTypeSymbol typeSymbol, Compilation compilation) {
        var memberRequirements = new Dictionary<string, (ISymbol Member, bool IsRequired)>();

        // 从当前类型开始，向上遍历继承链
        var currentType = typeSymbol;
        while (currentType != null) {
            foreach (var member in currentType.GetMembers()) {
                if (member is not IFieldSymbol and not IPropertySymbol)
                    continue;

                // 如果这个成员名称还没有被处理过（优先使用最派生类的定义）
                if (!memberRequirements.ContainsKey(member.Name)) {
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

            currentType = currentType.BaseType;
        }

        // 只返回标记为必需的成员
        return memberRequirements.Values
            .Where(requirement => requirement.IsRequired)
            .Select(requirement => requirement.Member);
    }

}
