using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LitSyntaxHighlighter
{
    internal partial class OptionsProvider
    {
        // Register the options with this attribute on your package class:
        // [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "LitSyntaxHighlighter", "General", 0, 0, true, SupportsProfiles = true)]
        [ComVisible(true)]
        public class GeneralOptionPage : BaseOptionPage<GeneralOptions> { }
    }

    public class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        [Category("General")]
        [DisplayName("Auto add close tags")]
        [Description("Automatically add a matching close tag when completing an empty open tag.")]
        [DefaultValue(true)]
        public bool AutoCloseTags { get; set; } = true;

        [Category("General")]
        [DisplayName("Auto rename closing tags")]
        [Description("Automatically rename close tags when updating its matching open tag.")]
        [DefaultValue(true)]
        public bool AutoRenameClosingTags { get; set; } = true;
    }
}
