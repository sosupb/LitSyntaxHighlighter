using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace LitSyntaxHighlighter
{
    /// <summary>
    /// Classifier provider. It adds the classifier to the set of classifiers.
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [ContentType("TypeScript")] // This classifier applies to all js and ts files.
    internal class LitClassifierProvider : IClassifierProvider
    {
        // Disable "Field is never assigned to..." compiler's warning. Justification: the field is assigned by MEF.
#pragma warning disable 649

        /// <summary>
        /// Classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
        private IClassificationTypeRegistryService classificationRegistry;

        [Import]
        internal IClassifierAggregatorService classifierAggregator;

#pragma warning restore 649

        #region IClassifierProvider

        private static bool createdClassifier = false;

        /// <summary>
        /// Gets a classifier for the given text buffer.
        /// </summary>
        /// <param name="buffer">The <see cref="ITextBuffer"/> to classify.</param>
        /// <returns>A classifier for the text buffer, or null if the provider cannot do so in its current state.</returns>
        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            if (createdClassifier)
            {
                return null;
            }

            try
            {
                if (classificationRegistry == null)
                {
                    throw new NullReferenceException(nameof(classificationRegistry));
                }

                createdClassifier = true;

                return buffer.Properties.GetOrCreateSingletonProperty(creator: () => new LitClassifier(classificationRegistry, classifierAggregator.GetClassifier(buffer)));
            }
            finally
            {
                createdClassifier = false;
            }
        }

        #endregion
    }
}
