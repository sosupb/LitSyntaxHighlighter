using LitSyntaxHighlighter.Utility;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace LitSyntaxHighlighter
{
    // When JS file is opened, the format definitions are created
    // Closing and reopen JS file, doesn't recreate the definitions

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Delimiter)]
    [Name(FormatNames.Delimiter)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class LitDelimiterFormatDefinition : ClassificationFormatDefinition
    {
        public LitDelimiterFormatDefinition()
        {
            this.DisplayName = "Lit Delimiter Character (String Literal)";
            this.ForegroundColor = ThemeUtility.IsLightTheme ? Colors.Blue : Colors.Silver;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Element)]
    [Name(FormatNames.Element)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class LitElementFormatDefinition : ClassificationFormatDefinition
    {

        public LitElementFormatDefinition()
        {
            this.DisplayName = "Lit Element (String Literal)";
            this.ForegroundColor = ThemeUtility.IsLightTheme ? Color.FromRgb(128, 0 ,0) : Color.FromRgb(86, 156, 214);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.AttributeName)]
    [Name(FormatNames.AttributeName)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class LitAttributeNameFormatDefinition : ClassificationFormatDefinition
    {
        public LitAttributeNameFormatDefinition()
        {
            this.DisplayName = "Lit Attribute Name (String Literal)";
            this.ForegroundColor = ThemeUtility.IsLightTheme ? Colors.Red : Color.FromRgb(156, 220, 254);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.AttributeDelimiter)]
    [Name(FormatNames.AttributeDelimiter)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class LitAttributeDelimiterFormatDefinition : ClassificationFormatDefinition
    {
        public LitAttributeDelimiterFormatDefinition()
        {
            this.DisplayName = "Lit Attribute Delimiter (String Literal)";
            this.ForegroundColor = ThemeUtility.IsLightTheme ? Colors.Purple : Colors.Purple;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.EventName)]
    [Name(FormatNames.EventName)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class LitEventNameFormatDefinition : ClassificationFormatDefinition
    {
        public LitEventNameFormatDefinition()
        {
            this.DisplayName = "Lit Event Name (String Literal)";
            this.ForegroundColor = ThemeUtility.IsLightTheme ? Color.FromRgb(116, 83, 31) : Color.FromRgb(220, 220, 170);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Quote)]
    [Name(FormatNames.Quote)]
    [UserVisible(true)]
    [Order(After = Priority.Low)]
    internal sealed class LitQuoteFormatDefinition : ClassificationFormatDefinition
    {
        public LitQuoteFormatDefinition()
        {
            this.DisplayName = "Lit Quote (String Literal)";
            this.ForegroundColor = ThemeUtility.IsLightTheme ? Colors.Black : Color.FromRgb(214, 157, 133);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.AttributeValue)]
    [Name(FormatNames.AttributeValue)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class LitAttributeValueFormatDefinition : ClassificationFormatDefinition
    {
        public LitAttributeValueFormatDefinition()
        {
            this.DisplayName = "Lit Attribute Value (String Literal)";
            this.ForegroundColor = ThemeUtility.IsLightTheme ? Colors.Black : Color.FromRgb(214, 157, 133);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Text)]
    [Name(FormatNames.Text)]
    [UserVisible(true)]
    [Order(After = Priority.Low)]
    internal sealed class LitTextFormatDefinition : ClassificationFormatDefinition
    {
        public LitTextFormatDefinition()
        {
            this.DisplayName = "Lit Text (String Literal)";
            this.ForegroundColor = ThemeUtility.IsLightTheme ? Colors.Black : Color.FromRgb(210, 210, 210);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Comment)]
    [Name(FormatNames.Comment)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class LitCommentFormatDefinition : ClassificationFormatDefinition
    {
        public LitCommentFormatDefinition()
        {
            this.DisplayName = "Lit Comment (String Literal)";
            this.ForegroundColor = ThemeUtility.IsLightTheme ? Colors.Green : Color.FromRgb(87, 166, 74);
        }
    }
}
