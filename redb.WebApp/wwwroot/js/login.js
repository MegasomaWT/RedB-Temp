$(() => {
    const stylingMode = 'outlined';
    const labelMode = 'static';
    DevExpress.config({
        editorStylingMode: stylingMode,
    });

    $('#dlogin').dxTextBox({
        height: '50px',
        widht: '200px',
        placeholder: 'type user\\email...',
        inputAttr: {
            autofocus: true,
            'autocomplete': "username",
            'aria-required': "true",
            // 'placeholder': "name@example.com",
            'type': "email",
            'focused': true,
            'data-val': "true",
            'data-val-email': "The Email field is not a valid e-mail address.",
            'data-val-required': "The Email field is required.",
            'id': "Input_Email",
            'name': "Input.Email",
            'value': ""
        },
        label: 'user\\email',
        labelMode: 'floating',
    });


    $('#dpassword').dxTextBox({
        height: '50px',
        widht: '200px',
        mode: 'password',
        placeholder: 'type password...',
        inputAttr: {
            'autocomplete': "current-password",
            'aria-required': "true",
            'placeholder': "password",
            'type': "password",
            'data-val': "true",
            'data-val-required': "The Password field is required.",
            'id': "Input_Password",
            'name': "Input.Password"
        },
        label: 'password',
        labelMode: 'floating',
    });

    $('#dsubmit').dxButton({
        type: 'success',
        text: 'Sign in',
        useSubmitBehavior: true,
        elementAttr: {
            'id': "login-submit",
            'type': "submit"
        }
    });

});