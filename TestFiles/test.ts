const testComponent = ("test-component") => {
    const testString = "This is a test string <a>.";

    const testTemplate = html`
        <some-other-component>
            <div class="stuff" style="style: 10px;"><test-component
                .prop="test"
                .prop2=${"test2"}
                ?prop3=${true}
                @propFunc=${() => html`<a>Sub Content</a>`}
            >Visible Content</test-component>
                <br /><!-- Comment Test -->
            </div>
        </some-other-component>
    `;

    const renderSubSection() {
        return html`
            <div>
                <sub-component-test-render
                    prop="prop"
                    .prop1=${"some value"}
                >
                    <!-- This is a self closing component -->
                    <self-closing-component-test/>
                </sub-component-test-render>
            </div>
        `;
    }

    const render() {
        return html`
            <wrapper-component>
                ${testTemplate}
            </wrapper-component>
        `;
    }
}