var transType = $('#transType').val();
var sentTo = $("#sentTo").val();
var resultReason = "";
var fingprint = "";
var errorId = $('#errorId').val();
var TYPE = $('#type').val();
var RENTSCENE = $('#rs').val();
var COMPANY = "Nooch";
var memIdGen = "";

$(document).ready(function () {
    var isSmScrn = false;
    if ($(window).width() < 768) {
        isSmScrn = true;
    }

    if (RENTSCENE == "yes") {
        COMPANY = "Rent Scene";

        $('.landingHeaderLogo').attr('href', 'http://www.rentscene.com');

        if (isSmScrn) {
            $('.landingHeaderLogo img').attr('src', 'https://noochme.com/noochweb/Assets/Images/rentscene.png').css('width', '130px');
        }
        else {
            $('.landingHeaderLogo img').attr('src', 'https://noochme.com/noochweb/Assets/Images/rentscene.png').css('width', '170px');
        }
    }

    if (TYPE == "personal" || TYPE == "2")
    {
        var fontSize = isSmScrn ? '18px' : '20px';

        $('#nameInNavContainer h5').removeClass('m-t-5').css({
            'font-size': fontSize,
            'font-weight': '600',
            'margin-top': '25px'
        })

        $('#nameInNav').hide();
    }

    if (areThereErrors() == false) {
        if (transType == "phone") {
            usrEm = "";
            usrPhn = sentTo;
        }
        else {
            usrEm = sentTo;
            usrPhn = "";
        }

        var confirmBtnTxt = isSmScrn ? "Let's Go!": "Great, Let's Go!";

        swal({
            title: "Secure, Private, & Direct Payments",
            text: "<p>Send money without having to leave your seat! &nbsp;Secure, direct, and traceable payments when you want them. &nbsp;Please verify your identity as an authorized user of your company's account.</p>" +
                  "<ul class='fa-ul'>" +
                  "<li><i class='fa-li fa fa-check'></i>We don't see or store your bank credentials</li>" +
                  "<li><i class='fa-li fa fa-check'></i>We use <strong>bank-grade encryption</strong> to secure all data</li>" +
                  "<li><i class='fa-li fa fa-check'></i>The recipient will never see your bank credentials</li></ul>",
            imageUrl: "../Assets/Images/secure.svg",
            imageSize: "194x80",
            showCancelButton: false,
            cancelButtonText: "Learn More",
            confirmButtonColor: "#3fabe1",
            confirmButtonText: confirmBtnTxt,
            closeOnCancel: false,
            customClass: "securityAlert",
            allowEscapeKey: false,
            html: true
        }, function (isConfirm) {
            if (isConfirm) {
                //Get Fingerprint
                new Fingerprint2().get(function (result) {
                    fingprint = result;
                    //console.log(fingprint);
                });

                setTimeout(function () {
                    $('input#idVer-name').focus();
                }, 800)
            }
            else {
                window.open("https://www.nooch.com/safe");
            }
            //console.log(ipusr);
        });

        // Now show the wizard since there are no errors (hidden on initial page load because it takes
        // a split second to format the Steps plugin, so it was visible as un-formatted code briefly.
        $('#idWizContainer').removeClass('hidden');
        runIdWizard();

        $('#relaunchIdwiz > button').click(function () {
            //Get Fingerprint Again
            new Fingerprint2().get(function (result) {
                fingprint = result;
            });

            $('#relaunchIdwiz').removeClass('bounceIn').addClass('bounceOut');

            runIdWizard()
        });
    }
    else {
        console.log("93. There was an error! :-(");
    }
});


function runIdWizard() {

    // Setup the ID Verification Wizard
    // Wizard Plugin Reference: https://github.com/rstaib/jquery-steps/wiki/Settings
    $("#idVerWiz").steps({
        headerTag: "h3",
        bodyTag: "section",
        stepsOrientation: "horizontal",
        transitionEffect: 'slideLeft',
        transitionEffectSpeed: 400,

        /* Labels */
        labels: {
            finish: "Submit"
        },

        /* Events */
        onInit: function (event, currentIndex) {
            $('#idVerWiz > .content').animate({ height: "27em" }, 300)

            var DOB = $('#dob').val() ? $('#dob').val() : "1980 01 01";
            console.log(DOB);
            //console.log(moment(DOB, "YYYY MM DD"));
            $('#idVer-dob').datetimepicker({
                format: 'MM/DD/YYYY',
                useCurrent: false,
                defaultDate: DOB,// moment(DOB, "YYYY MM DD"),
                icons: {
                    previous: 'fa fa-fw fa-chevron-circle-left',
                    next: 'fa fa-fw fa-chevron-circle-right',
                    clear: 'fa fa-fw fa-trash-o'
                },
                maxDate: "1996-12-31",
                viewMode: 'years',
                //debug: true
            });

            $('#idVer-ssn').mask("0000");
            $('#idVer-zip').mask("00000");
            $('#idVer-phone').val(usrPhn);
            $('#idVer-email').val(usrEm);
            $('#idVer-phone').mask('(000) 000-0000');

            $('[data-toggle="popover"]').popover();


            var calendarIcon = $('#idVerForm1 .datePickerGrp i');

            calendarIcon.click(function ()
            {
                setTimeout(function () {
                    $('#dobGrp .dtp-container.dropdown').addClass('fg-toggled open');
                    $('#idVer-dob').data("DateTimePicker").show();
                }, 150);
            });


            if (TYPE == "1") // For existing users
            {
                console.log("Existing user!");

                var NAME = $('#name').val();
                var ADDRESS = $('#address').val();
                var ZIP = $('#zip').val();
                var EMAIL = $('#email').val();
                var PHONE = $('#phone').val();

                $('#idVer-name').val(NAME);
                $('#idVer-address').val(ADDRESS);
                $('#idVer-zip').val(ZIP);
                $('#idVer-email').val(EMAIL);
                $('#idVer-phone').val(PHONE).mask('(000) 000-0000');
            }
        },
        onStepChanging: function (event, currentIndex, newIndex) {

            if (currentIndex == 1) {
                $('.createAccountPg #idVerWiz').css('overflow', 'hidden');
            }

            if (newIndex == 0) {
                $('#idVerWiz > .content').animate({ height: "27em" }, 600)
            }

            // IF going to Step 2
            if (currentIndex == 0)
            {
                // Check Name field for length
                if ($('#idVer-name').val().length > 4)
                {
                    var trimmedName = $('#idVer-name').val().trim();
                    $('#idVer-name').val(trimmedName);

                    // Check Name Field for a " "
                    if ($('#idVer-name').val().indexOf(' ') > 1)
                    {
                        updateValidationUi("name", true);

                        // Check DOB field
                        if ($('#idVer-dob').val().length == 10)
                        {
                            updateValidationUi("dob", true);

                            // Check SSN field
                            if ($('#idVer-ssn').val().length == 4)
                            {
                                updateValidationUi("ssn", true);

                                // Great, we can finally go to the next step of the wizard :-]
                                $('#idVerWiz > .content').animate({ height: "20.5em" }, 600)
                                return true;
                            }
                            else {
                                updateValidationUi("ssn", false);
                            }
                        }
                        else {
                            updateValidationUi("dob", false);
                        }
                    }
                    else {
                        updateValidationUi("name", false);
                    }
                }
                else {
                    updateValidationUi("name", false);
                }

                return false;
            }

            // IF going to Step 3
            if (newIndex == 2)
            {
                var trimmedAddress = $('#idVer-address').val().trim();
                $('#idVer-address').val(trimmedAddress);

                // Check Address field
                if ($('#idVer-address').val().length > 4 &&
                    $('#idVer-address').val().indexOf(' ') > -1)
                {
                    updateValidationUi("address", true);

                    // Check ZIP code field
                    var trimmedZip = $('#idVer-zip').val().trim();
                    $('#idVer-zip').val(trimmedZip);

                    if ($('#idVer-zip').val().length == 5)
                    {
                        updateValidationUi("zip", true);

                        // Great, go to the next step of the wizard :-]

                        // FILE INPUT DOCUMENTATION: http://plugins.krajee.com/file-input#options
                        /*$("#IdWizPic_FileInput").fileinput({
                            allowedFileTypes: ['image'],
                            initialPreview: [
                                "<img src='' class='file-preview-image' alt='Profile Picture' id='IdWizUserPicPreview'>"
                            ],
                            initialPreviewShowDelete: false,
                            layoutTemplates: {
                                icon: '<span class="md md-panorama m-r-10 kv-caption-icon"></span>',
                            },
                            maxFileCount: 1,
                            maxFileSize: 250,
                            msgSizeTooLarge: "File '{name}' ({size} KB) exceeds the maximum allowed file size of {maxSize} KB. Please try a slightly smaller picture!",
                            showCaption: false,
                            showUpload: false,
                            //uploadUrl: URLs.UploadLandlordProfileImage,  // NEED TO ADD URL TO SERVICE FOR SAVING PROFILE PIC (SEPARATELY FROM SAVING THE REST OF THE PROFILE INFO)
                            //uploadExtraData: {
                                //LandlorId: $scope.userInfoInSession.memberId,
                                //AccessToken: $scope.userInfoInSession.accessToken
                            //},
                            showPreview: true,
                            resizeImage: true,
                            maxImageWidth: 400,
                            maxImageHeight: 400,
                            resizePreference: 'width'
                        });*/

                        $('#idVerWiz > .content').animate({ height: "20em" }, 500)
                        return true;
                    }
                    else {
                        updateValidationUi("zip", false);
                    }
                }
                else {
                    updateValidationUi("address", false);
                }
            }

            // Allways allow previous action even if the current form is not valid!
            if (currentIndex > newIndex) {
                return true;
            }
        },
        onStepChanged: function (event, currentIndex, priorIndex) {
            if (currentIndex == 1) {
                $('.createAccountPg #idVerWiz').css('overflow', 'visible');
                $('#idVer-address').focus();
            }
            else if (currentIndex == 2) {
                $('#idVer-email').focus();
            }
        },
        onCanceled: function (event) {
            cancelIdVer();
        },
        onFinishing: function (event, currentIndex) {
            // CHECK TO MAKE SURE ALL FIELDS WERE COMPLETED

            $('#idVer-email').val($('#idVer-email').val().trim());

            if (ValidateEmail($('#idVer-email').val()) == true) {
                updateValidationUi("email", true);

                // Finally, check the phone number's length
                console.log($('#idVer-phone').cleanVal());

                if ($('#idVer-phone').cleanVal().length == 10) {
                    updateValidationUi("phone", true);

                    // Finish the Wizard...
                    return true;
                }
                else {
                    updateValidationUi("phone", false);
                }
            }
            else {
                updateValidationUi("email", false);
            }
        },
        onFinished: function (event, currentIndex) {

            // ADD THE LOADING BOX
            $('#idWizContainer').block({
                message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Validating Your Info...</span>',
                css: {
                    border: 'none',
                    padding: '20px 8px 14px',
                    backgroundColor: '#000',
                    '-webkit-border-radius': '12px',
                    '-moz-border-radius': '12px',
                    'border-radius': '12px',
                    opacity: '.75',
                    width: '260px',
                    margin: '0 auto',
                    color: '#fff'
                }
            });

            // SUBMIT DATA TO NOOCH SERVER
            if (TYPE == "1") // For existing users
            {
                console.log("Existing user, so calling saveMemberInfo()")
                saveMemberInfo();
            }
            else
            {
                console.log("New user, so calling createRecord()")
                createRecord();
            }
        }
    });
}


function updateValidationUi(field, success) {
    console.log("Field: " + field + "; success: " + success);

    if (success == true) {
        $('#' + field + 'Grp .form-group').removeClass('has-error').addClass('has-success');
        $('#' + field + 'Grp .help-block').slideUp();

        // Show the success checkmark
        if (!$('#' + field + 'Grp .iconFeedback').length) {
            $('#' + field + 'Grp .form-group .fg-line').append('<i class="fa fa-check text-success iconFeedback animated bounceIn"></i>');
        }
        else {
            $('#' + field + 'Grp .iconFeedback').removeClass('bounceOut').addClass('bounceIn');
        }
    }
    else {
        $('#' + field + 'Grp .form-group').removeClass('has-success').addClass('has-error');

        // Hide the success checkmark if present
        if ($('#' + field + 'Grp .iconFeedback').length) {
            $('#' + field + 'Grp .iconFeedback').addClass('bounceOut');
        }

        var helpBlockTxt = "";
        if (field == "name") {
            helpBlockTxt = "Please enter your full <strong>legal name</strong>.";
        }
        else if (field == "dob") {
            helpBlockTxt = "Please enter your date of birth. &nbsp;Only needed to verify your ID!"
        }
        else if (field == "ssn") {
            helpBlockTxt = "Please enter just the <strong>LAST 4 digits</strong> of your SSN. This is used solely to protect your account."
            $('#idVerWiz > .content').animate({ height: "28em" }, 600)
        }
        else if (field == "address") {
            helpBlockTxt = "Please enter the physical street address of where you <strong>currently</strong> live."
            $('#idVerWiz > .content').animate({ height: "21em" }, 600);
        }
        else if (field == "zip") {
            helpBlockTxt = "Please enter the zip code for the street address above."
            $('#idVerWiz > .content').animate({ height: "21em" }, 600);
        }
        else if (field == "email") {
            helpBlockTxt = "Please enter a valid email address that you own."
        }
        else if (field == "phone") {
            helpBlockTxt = "Please make sure you enter a valid 10-digit phone nuber."
        }

        if (!$('#' + field + 'Grp .help-block').length) {
            $('#' + field + 'Grp .form-group').append('<small class="help-block" style="display:none">' + helpBlockTxt + '</small>');
            $('#' + field + 'Grp .help-block').slideDown();
        }
        else { $('#' + field + 'Grp .help-block').show() }

        // Now focus on the element that failed validation
        setTimeout(function () {
            $('#' + field + 'Grp input').focus();
        }, 200)
    }

}


function ValidateEmail(str) {
    var at = "@"
    var dot = "."
    var lat = str.indexOf(at)
    var lstr = str.length
    var ldot = str.indexOf(dot)

    if (lstr < 5) {
        return false;
    }

    if (lat == -1 || lat == 0 || lat == lstr) {
        return false
    }

    if (ldot == -1 || ldot == 0 || ldot > lstr - 3) {
        return false
    }

    if (str.indexOf(at, (lat + 1)) != -1) {
        return false
    }

    if (str.substring(lat - 1, lat) == dot || str.substring(lat + 1, lat + 2) == dot) {
        return false
    }

    if (str.indexOf(dot, (lat + 2)) == -1) {
        return false
    }

    if (str.indexOf(" ") != -1) {
        return false
    }

    return true
};


function createRecord() {
    console.log('createRecord Initiated...');

    var userEmVal = $('#idVer-email').val();
    var userPhVal = $('#idVer-phone').cleanVal();
    var userNameVal = $('#idVer-name').val().trim();
    var userPwVal = "";  // Still need to add the option for users to create a PW (not sure where in the flow to do it)
    var ssnVal = $('#idVer-ssn').val().trim();
    var dobVal = $('#idVer-dob').val().trim();
    var addressVal = $('#idVer-address').val().trim();
    var zipVal = $('#idVer-zip').val().trim();
    var fngprntVal = fingprint;
    var ipVal = ipusr;

    console.log("CREATE RECORD -> {userEm: " + userEmVal + ", userPh: " + userPhVal +
                                ", userName: " + userNameVal + ", userPw: " + userPwVal +
                                ", ssn: " + ssnVal + ", dob: " + dobVal +
                                ", address: " + addressVal + ", zip: " + zipVal +
                                ", fngprnt: " + fngprntVal + ", ip: " + ipVal + "}");

    $.ajax({
        type: "POST",
        url: "createAccount.aspx/CreateAccountInDB",
        data: "{'name':'" + userNameVal +
              "', 'dob':'" + dobVal +
              "', 'ssn':'" + ssnVal +
              "', 'address':'" + addressVal +
              "', 'zip':'" + zipVal +
              "', 'email':'" + userEmVal +
              "', 'phone':'" + userPhVal +
              "', 'fngprnt':'" + fngprntVal +
              "', 'ip':'" + ipVal +
              "', 'pw':'" + '' + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {

            var CreateAccountInDbRes = msg.d;
            console.log("SUCCESS -> 'CreateAccountInDB' Result is... ");
            console.log(CreateAccountInDbRes);

            // Hide the Loading Block
            $('.modal-content').unblock();

            if (CreateAccountInDbRes.success == true &&
			    CreateAccountInDbRes.note.length > 5)
            {
                memIdGen = CreateAccountInDbRes.note.trim();

                // HIDE THE WIZARD
                $('#idWizContainer').slideUp()

                // THEN DISPLAY SUCCESS ALERT...
                swal({
                    title: "Great Job!",
                    text: "<i class=\"mdi mdi-account-check text-success\"></i><br/>Thanks for submitting your ID information. That helps us keep Nooch safe for everyone." +
                           "<span>Next, link any checking account to complete this payment:</span>" +
                           "<span class=\"spanlist\"><span>1. &nbsp;Select your bank</span><span>2. &nbsp;Login with your regular online banking credentials</span><span>3. &nbsp;Choose which account to use</span></span>",
                    type: "success",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Continue",
                    closeOnConfirm: true,
                    html: true,
                    customClass: "idVerSuccessAlert",
                });

                $('#AddBankDiv').removeClass('hidden');

                $("#frame").attr("src", "https://www.noochme.com/noochweb/trans/Add-Bank.aspx?MemberId=" + CreateAccountInDbRes.note + "&redUrl=createaccnt");
            }
            else if (CreateAccountInDbRes.msg.indexOf("already a"))
            {
                showErrorAlert('20');
            }
            else
            {
                showErrorAlert('2');
            }
        },
        Error: function (x, e) {
            // Hide UIBlock (loading box)) 
            // Hide the Loading Block
            console.log("ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('2');
        }
    });
}


function saveMemberInfo() {
    console.log('saveMemberInfo got called');

    var memId = $('#memId').val();
    var userNameVal = $('#idVer-name').val().trim();
    var userEmVal = $('#idVer-email').val();
    var userPhVal = $('#idVer-phone').cleanVal();
    var userPwVal = "";  // Still need to add the option for users to create a PW (not sure where in the flow to do it)
    var ssnVal = $('#idVer-ssn').val().trim();
    var dobVal = $('#idVer-dob').val().trim();
    var addressVal = $('#idVer-address').val().trim();
    var zipVal = $('#idVer-zip').val().trim();
    var fngprntVal = fingprint;
    var ipVal = ipusr;

    console.log("SAVE MEMBER INFO -> {memId: " + memId +
                                   ", Name: " + userNameVal +
                                   ", dob: " + dobVal +
                                   ", ssn: " + ssnVal +
                                   ", address: " + addressVal +
                                   ", zip: " + zipVal +
                                   ", email: " + userEmVal +
                                   ", phone: " + userPhVal +
                                   ", fngprnt: " + fngprntVal +
                                   ", ip: " + ipVal + "}");

    $.ajax({
        type: "POST",
        url: "createAccount.aspx/saveMemberInfo",
        data: "{'memId':'" + memId +
             "', 'name':'" + userNameVal +
             "', 'dob':'" + dobVal +
             "', 'ssn':'" + ssnVal +
             "', 'address':'" + addressVal +
             "', 'zip':'" + zipVal +
             "', 'email':'" + userEmVal +
             "', 'phone':'" + userPhVal +
             "', 'fngprnt':'" + fngprntVal +
             "', 'ip':'" + ipVal + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {

            var result = msg.d;
            console.log("SUCCESS -> Save Member Info result is... [next line]");
            console.log(result);

            resultReason = result.msg;

            // Hide the Loading Block
            $('#idWizContainer').unblock();

            if (result.success == true)
            {
                console.log("Success == true");

                // HIDE THE MODAL CONTAINING THE WIZARD
                $('#idVer').modal('hide')

                // THEN DISPLAY SUCCESS ALERT...
                swal({
                    title: "Submitted Successfully",
                    text: "<i class=\"mdi mdi-account-check text-success\"></i><br/>Thanks for submitting your ID information. That helps us keep " + COMPANY + " safe for everyone. &nbsp;We only use this information to prevent ID fraud and never share it without your permission.",
                           //"<span>Next, link any checking account to complete this payment:</span>" +
                           //"<span class=\"spanlist\"><span>1. &nbsp;Select your bank</span><span>2. &nbsp;Login with your regular online banking credentials</span><span>3. &nbsp;Choose which account to use</span></span>",
                    type: "success",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Great!",
                    closeOnConfirm: true,
                    html: true,
                    customClass: "idVerSuccessAlert",
                });

                $('#idWizContainer').fadeOut(600);

                $('.resultDiv').removeClass('hidden');
                //$('#AddBankDiv').removeClass('hidden');

                //$("#frame").attr("src", "https://www.noochme.com/noochweb/trans/Add-Bank.aspx?MemberId=" + RegisterUserWithSynpResult.memberIdGenerated + "&redUrl=https://www.noochme.com/noochweb/trans/payRequestComplete.aspx?mem_id=" + RegisterUserWithSynpResult.memberIdGenerated + "," + transIdVal);
            }
            else
            {
                console.log("Success != true");

                if (resultReason != null)
                {
                    if (resultReason.indexOf("email already registered") > -1) {
                        showErrorAlert('20');
                    }
                    else if (resultReason.indexOf("phone number already registered") > -1) {
                        showErrorAlert('30');
                    }
                    else if (resultReason.indexOf("Missing critical data") > -1) {
                        showErrorAlert('2');
                    }
                }
                else
                {
                    console.log("Error checkpoint # 659");
                    showErrorAlert('3');
                }
            }
        },
        Error: function (x, e) {
            // Hide the Loading Block
            $('#idWizContainer').unblock();

            console.log("ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('3');
        }
    });
}


// To handle success from Add-Bank page (CC: 1/20/16)
$('body').bind('addBankComplete', function ()
{
    $('#AddBankDiv').slideUp();

    swal({
        title: "Bank Linked Successfully",
        text: "<i class=\"fa fa-bank text-success\"></i>" +
              "<span class='m-t-10'>Your bank account is now linked!" +
              "<span>Enter a password below if you'd like to be able to make future payments without re-entering all your info. <em>Optional</em></span>",
        type: "input",
        inputType: "password",
        inputPlaceholder: "Password",
        showCancelButton: true,
        confirmButtonColor: "#3fabe1",
        confirmButtonText: "Submit",
        cancelButtonText: "No thanks",
        closeOnConfirm: false,
        html: true
    }, function (inputValue)
    {
        if (inputValue === false) return false;

        if (inputValue === "")
        {
            swal.showInputError("You can't have a blank password unfortunately!");
            return false
        }
        if (inputValue.length < 6)
        {
            swal.showInputError("For security, please choose a slightly longer password!");
            return false
        }

        var dataToSend = "";

        if (typeof memIdGen == "string" && memIdGen.length > 30) // make sure it's not 'undefined' or 'object' (for NULL)
        {
            dataToSend = "{'newPw':'" + inputValue.trim() +
                         "', 'memId':'" + memIdGen + "'}";
        }
        else
        {
            return false;
        }

        // ADD THE LOADING BOX
        $('#idWizContainer').block({
            message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Validating Your Info...</span>',
            css: {
                border: 'none',
                padding: '20px 8px 14px',
                backgroundColor: '#000',
                '-webkit-border-radius': '12px',
                '-moz-border-radius': '12px',
                'border-radius': '12px',
                opacity: '.75',
                'z-index': '99999',
                margin: '0 auto',
                color: '#fff'
            }
        });

        var urlToUse = "";
        if (transType == "send")
        {
            urlToUse = "depositMoney.aspx/setPw";
        }
        else // must be a Request or Rent payment (which also uses the payRequest page)
        {
            urlToUse = "payRequest.aspx/setPw";
        }

        $.ajax({
            type: "POST",
            url: "createAccount.aspx/setPw",
            data: dataToSend,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            async: "true",
            cache: "false",
            success: function (msg)
            {
                var setPw = msg.d;
                console.log("SUCCESS -> 'RegisterUserWithSynpResult' is... ");
                console.log(setPw);

                // Hide the Loading Block
                $('.modal-content').unblock();

                swal({
                    title: "Great Job!",
                    text: "You can now login to the Nooch iOS app to create payments on the go." +
                          "<span class='show m-t-10'>Don't have an iPhone? You can still accept payments (or pay requests) from any browser - the other party just needs to know your email address or phone number, that's it!</span>",
                    type: "success",
                    showCancelButton: true,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Download App",
                    cancelButtonText: "Close",
                    html: true
                }, function (isConfirm) {
                    if (isConfirm)
                    {
                        window.location = "https://geo.itunes.apple.com/us/app/nooch/id917955306?mt=8";
                    }
                });
            },
            Error: function (x, e)
            {
                // Hide the Loading Block
                $('.modal-content').unblock();
                console.log("ERROR --> 'x', then 'e' is... ");
                console.log(x);
                console.log(e);

                showErrorAlert('2');
            }

        });
    });
});


function areThereErrors() {

    if (errorId != "0")
    {
        console.log('areThereErrors -> errorId is: [' + errorId + "]");

        // Position the footer absolutely so it's at the bottom of the screen (it's normally pushed down by the body content)
        $('.footer').css({
            position: 'fixed',
            bottom: '5%'
        })

        showErrorAlert(errorId);

        return true;
    }

    console.log("No Errors!");
    return false;
}


function showErrorAlert(errorNum) {
    var alertTitle = "";
    var alertBodyText = "";
    var shouldFocusOnEmail = false;
    var shouldFocusOnPhone = false;
    var shouldShowErrorDiv = true;

    console.log("ShowError -> errorNum is: [" + errorNum + "], resultReason is: [" + resultReason + "]");

    if (errorNum == '1') // Codebehind errors
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "We had trouble finding that transaction.  Please try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '2' || errorNum == '3') // Errors after submitting ID verification AJAX
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing your info.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '20') // Submitted ID Verification info, but EMAIL came back as already registered with Nooch.
    {
        alertTitle = "Email Already Registered";
        alertBodyText = "Looks like <strong>" + $('#idVer-email').val() + "</strong> is already registered to a Nooch account. &nbsp;Please try a different email address.";
        shouldFocusOnEmail = true;
        shouldShowErrorDiv = false;
    }
    else if (errorNum == '30') // Submitted ID Verification info, but PHONE came back as already registered with Nooch.
    {
        alertTitle = "Phone Number Already Registered";
        alertBodyText = "Looks like <strong>" + $('#idVer-phone').val() + "</strong> is already registered to a Nooch account. &nbsp;Please try a different number.";
        shouldFocusOnPhone = true;
        shouldShowErrorDiv = false;
    }
    else // Generic Error
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing your request. &nbsp;Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    console.log("Should Show Error Div is...");
    console.log(shouldShowErrorDiv);

    if (shouldShowErrorDiv == true) {
        $(".errorMessage").removeClass('hidden');
        $(".errorMessage").html(alertBodyText);
        $(".errorMessage a").addClass("btn btn-default m-t-20 animated bounceIn");
    }
    else {
        $(".errorMessage").addClass('hidden');
    }

    swal({
        title: alertTitle,
        text: alertBodyText,
        type: "error",
        showCancelButton: true,
        confirmButtonColor: "#3fabe1",
        confirmButtonText: "Ok  :-(",
        cancelButtonText: "Contact Support",
        closeOnConfirm: true,
        closeOnCancel: false,
        allowEscapeKey: false,
        html: true
    }, function (isConfirm) {
        if (!isConfirm) {
            window.open("mailto:support@nooch.com");
        }
        else if (shouldFocusOnEmail) {
            updateValidationUi("email", false);
        }
        else if (shouldFocusOnPhone) {
            updateValidationUi("phone", false);
        }
    });
}


$('body').on('focus', '.form-control', function () {
    $(this).closest('.fg-line').addClass('fg-toggled');
})

$('body').on('blur', '.form-control', function () {
    var fgrp = $(this).closest('.form-group');
    var ipgrp = $(this).closest('.input-group');

    var val = fgrp.find('.form-control').val();
    var val2 = ipgrp.find('.form-control').val();

    if (fgrp.hasClass('fg-float')) {
        if (val.length == 0) {
            $(this).closest('.fg-line').removeClass('fg-toggled');
        }
    }
    else if (ipgrp.hasClass('fg-float')) {
        if (val2.length == 0) {
            $(this).closest('.fg-line').removeClass('fg-toggled');
        }
    }
    else {
        $(this).closest('.fg-line').removeClass('fg-toggled');
    }
});
