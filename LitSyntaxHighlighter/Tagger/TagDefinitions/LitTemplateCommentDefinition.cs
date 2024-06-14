using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace LitSyntaxHighlighter.Tagger.TagDefinitions
{
    internal static class LitTemplateCommentClassificationDefinition
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.CommentDark)]
        internal static ClassificationTypeDefinition CommentDark = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.CommentLight)]
        internal static ClassificationTypeDefinition CommentLight = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.CommentDark)]
    [Name(LitClassificationNames.CommentDark)]
    [UserVisible(true)]
    internal class LitTemplateCommentDarkFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateCommentDarkFormatDefinition()
        {
            this.DisplayName = "Lit Comment Dark Character (String Literal)";
            this.ForegroundColor = Color.FromRgb(87, 166, 74);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.CommentLight)]
    [Name(LitClassificationNames.CommentLight)]
    [UserVisible(true)]
    internal class LitTemplateCommentLightFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateCommentLightFormatDefinition()
        {
            this.DisplayName = "Lit Comment Light Character (String Literal)";
            this.ForegroundColor = Colors.Green;
        }
    }
}
