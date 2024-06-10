    const testComponent = () => {
    const testString = "This is a test string <a>.";
    const testTemplate = html`
        <div class="stuff" style="morestuff"><test-component
            .prop="toast"
            .prop2=${"toast2"}
            ?prop3=${"toast3"}
            @propFunc=${html`<a></a>`}
        >Steve</test-component>
            <br />${"SAtevssd f fas <a>df a"}
        </div>
    `;
}