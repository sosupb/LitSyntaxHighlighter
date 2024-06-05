using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace HtmlSyntaxHighlighter
{
    #region Format definition

    // When JS file is opened, the format definitions are created
    // Closing and reopen JS file, doesn't recreate the definitions

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Delimiter)]
    [Name(FormatNames.Delimiter)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class HtmlDelimiterFormatDefinition : ClassificationFormatDefinition
    {
        public HtmlDelimiterFormatDefinition()
        {
            this.DisplayName = "HTML Delimiter Character (JS String Literal)";
            this.ForegroundColor = Colors.Blue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Element)]
    [Name(FormatNames.Element)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class HtmlElementFormatDefinition : ClassificationFormatDefinition
    {

        public HtmlElementFormatDefinition()
        {
            this.DisplayName = "HTML Element (JS String Literal)";
            this.ForegroundColor = Color.FromRgb(128, 0, 0);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.AttributeName)]
    [Name(FormatNames.AttributeName)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class HtmlAttributeNameFormatDefinition : ClassificationFormatDefinition
    {
        public HtmlAttributeNameFormatDefinition()
        {
            this.DisplayName = "HTML Attribute Name (JS String Literal)";
            this.ForegroundColor = Colors.Red;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Quote)]
    [Name(FormatNames.Quote)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class HtmlQuoteFormatDefinition : ClassificationFormatDefinition
    {
        public HtmlQuoteFormatDefinition()
        {
            this.DisplayName = "HTML Quote (JS String Literal)";
            this.ForegroundColor = Colors.Black;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.AttributeValue)]
    [Name(FormatNames.AttributeValue)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class HtmlAttributeValueFormatDefinition : ClassificationFormatDefinition
    {
        public HtmlAttributeValueFormatDefinition()
        {
            this.DisplayName = "HTML Attribute Value (JS String Literal)";
            this.ForegroundColor = Colors.Blue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Text)]
    [Name(FormatNames.Text)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class HtmlTextFormatDefinition : ClassificationFormatDefinition
    {
        public HtmlTextFormatDefinition()
        {
            this.DisplayName = "HTML Text (JS String Literal)";
            this.ForegroundColor = Colors.Black;
        }
    }

    #endregion //Format definition
}
