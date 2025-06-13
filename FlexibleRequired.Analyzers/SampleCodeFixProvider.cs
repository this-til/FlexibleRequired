using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace FlexibleRequired.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RequiredMemberCodeFixProvider)), Shared]
public class RequiredMemberCodeFixProvider : CodeFixProvider {

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RequiredMemberAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        Diagnostic diagnostic = context.Diagnostics.First();
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        if (root?.FindNode(diagnosticSpan) is ObjectCreationExpressionSyntax objectCreation) {
            // 获取缺失的成员名称列表
            string memberNamesString = diagnostic.Properties.TryGetValue("MemberNames", out var names) 
                ? names ?? string.Empty
                : string.Empty;

            if (string.IsNullOrEmpty(memberNamesString)) {
                return;
            }

            var memberNames = memberNamesString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var displayNames = string.Join(", ", memberNames.Select(name => $"'{name}'"));

            context.RegisterCodeFix
            (
                Microsoft.CodeAnalysis.CodeActions.CodeAction.Create
                (
                    title: $"Initialize required member(s) {displayNames}",
                    createChangedDocument: c => AddMemberInitializationsAsync
                    (
                        context.Document,
                        objectCreation,
                        memberNames,
                        c
                    ),
                    equivalenceKey: $"InitializeRequiredMembers_{string.Join("_", memberNames)}"
                ),
                diagnostic
            );
        }
    }

    private async Task<Document> AddMemberInitializationsAsync
    (
        Document document,
        ObjectCreationExpressionSyntax objectCreation,
        string[] memberNames,
        CancellationToken cancellationToken
    ) {
        // 获取语义模型以确定成员类型
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        var typeInfo = semanticModel?.GetTypeInfo(objectCreation);
        var typeSymbol = typeInfo?.Type as INamedTypeSymbol;

        // 为每个缺失的成员创建初始化表达式
        var newInitializers = new List<ExpressionSyntax>();

        foreach (var memberName in memberNames) {
            // 查找成员的类型信息
            var member = typeSymbol?.GetMembers(memberName).FirstOrDefault();
            ExpressionSyntax defaultValue;

            if (member is IPropertySymbol property) {
                defaultValue = CreateDefaultValueExpression(property.Type);
            } else if (member is IFieldSymbol field) {
                defaultValue = CreateDefaultValueExpression(field.Type);
            } else {
                // 如果无法确定类型，使用通用的 default 表达式
                defaultValue = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword));
            }

            // 创建初始化表达式
            var initializer = SyntaxFactory.AssignmentExpression
            (
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(memberName),
                defaultValue
            );

            newInitializers.Add(initializer);
        }

        // 创建或更新初始化器
        InitializerExpressionSyntax newInitializerList;
        if (objectCreation.Initializer == null) {
            newInitializerList = SyntaxFactory.InitializerExpression
            (
                SyntaxKind.ObjectInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(newInitializers)
            );
        }
        else {
            var allExpressions = objectCreation.Initializer.Expressions.AddRange(newInitializers);
            newInitializerList = objectCreation.Initializer.WithExpressions(allExpressions);
        }

        // 替换原有对象创建表达式
        var newObjectCreation = objectCreation
            .WithInitializer(newInitializerList)
            .WithAdditionalAnnotations(Formatter.Annotation);

        // 替换文档中的节点
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        var newRoot = root!.ReplaceNode(objectCreation, newObjectCreation);

        return document.WithSyntaxRoot(newRoot);
    }

    private static ExpressionSyntax CreateDefaultValueExpression(ITypeSymbol typeSymbol) {
        return typeSymbol.SpecialType switch {
            SpecialType.System_String => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("")),
            SpecialType.System_Int32 => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)),
            SpecialType.System_Boolean => SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression),
            SpecialType.System_Double => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0.0)),
            SpecialType.System_Single => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0.0f)),
            SpecialType.System_Int64 => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0L)),
            _ => SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
        };
    }

}
