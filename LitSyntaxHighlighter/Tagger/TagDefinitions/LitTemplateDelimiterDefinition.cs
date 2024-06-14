using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace LitSyntaxHighlighter.Tagger.TagDefinitions
{
    internal static class LitTemplateDelimiterClassificationDefinition
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.DelimiterDark)]
        internal static ClassificationTypeDefinition DelimiterDark = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.DelimiterLight)]
        internal static ClassificationTypeDefinition DelimiterLight = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.DelimiterDark)]
    [Name(LitClassificationNames.DelimiterDark)]
    [UserVisible(true)]
    internal class LitTemplateDelimiterDarkFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateDelimiterDarkFormatDefinition()
        {
            this.DisplayName = "Lit Delimiter Dark Character (String Literal)";
            this.ForegroundColor = Colors.Silver;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.DelimiterLight)]
    [Name(LitClassificationNames.DelimiterLight)]
    [UserVisible(true)]
    internal class LitTemplateDelimiterLightFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateDelimiterLightFormatDefinition()
        {
            this.DisplayName = "Lit Delimiter Light Character (String Literal)";
            this.ForegroundColor = Colors.Blue;
        }
    }
}
