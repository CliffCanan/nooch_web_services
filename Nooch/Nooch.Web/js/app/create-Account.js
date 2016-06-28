var transType = $('#transType').val();
var sentTo = $("#sentTo").val();
var resultReason = "";
var fingprint = "";
var errorId = $('#errorId').val();
var TYPE = $('#type').val();
var RENTSCENE = $('#rs').val();
var COMPANY = "Nooch";
var memIdGen = "";
var memid = $('#memId').val();
var ISNEW = $('#isNew').val();
var FBID = "not connected";

var isUpgradeToV3 = 'false';

// to be used with upload doc related stuff
var isFileAdded = "0";
var FileData = null;

var isIdVerified = false;
var isSmScrn = false;

$(document).ready(function () {
    if ($(window).width() < 768) {
        isSmScrn = true;
    }

    function getParameterByName(name) {
        name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
        var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
            results = regex.exec(location.search);
        return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
    }

    isUpgradeToV3 = getParameterByName('update');

    console.log("isUpgradeToV3 is: [" + isUpgradeToV3 + "]");

    if (RENTSCENE == "true")
    {
        COMPANY = "Rent Scene";

        var w = isSmScrn ? '90px' : '130px'
        $('.landingHeaderLogo img').css('width', w);

        if (ISNEW == "true")
        {
            document.title = "Create Account | Rent Scene Payments";
        }
        else
        {
            document.title = "Update Account | Rent Scene Payments";
        }
        changeFavicon('../Assets/favicon2.ico');
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

    if (areThereErrors() == false)
    {
        if (transType == "phone") {
            usrEm = "";
            usrPhn = sentTo;
        }
        else {
            usrEm = sentTo;
            usrPhn = "";
        }

        // Get Fingerprint
        new Fingerprint2().get(function (result) {
            fingprint = result;
            //console.log(fingprint);
        });
        //console.log(ipusr);

        if (isUpgradeToV3 == 'true')
        {
            swal({
                title: "Security Upgrade Notice",
                text: "<p>" + COMPANY + " offers a quick, secure way to pay anyone without giving them your sensitive bank or credit card information. &nbsp;Just select your bank and login to your online banking<span class='desk-only'> as you normally do</span>.</p>" +
                      "<h4><strong class='text-success'>Benefits</strong></h4>" +
                      "<ul class='fa-ul'><li><i class='fa-li fa fa-check'></i><strong>Faster Payments.</strong>&nbsp; Funds available by the 2nd day.</li>" +
                      "<li><i class='fa-li fa fa-check'></i><strong>Enhanced Security.</strong>&nbsp; This upgrade includes major under-the-hood updates to keep your money safe.</li>" +
                      "<li><i class='fa-li fa fa-check'></i><strong>Clearer Statements.</strong>&nbsp; Now the memo on your bank statement will include transaction info.</li></ul>",
                imageUrl: "../Assets/Images/secure.svg",
                imageSize: "194x80",
                showCancelButton: true,
                cancelButtonText: "Learn More",
                closeOnCancel: false,
                confirmButtonColor: "#3fabe1",
                confirmButtonText: "Let's Go!",
                customClass: "upgradeNotice",
                allowEscapeKey: false,
                html: true
            }, function (isConfirm)
            {
                if (isConfirm)
                {
                    setTimeout(function () {
                        $('input#idVer-name').focus();
                    }, 500)
                }
                else
                {
                    window.open("https://www.nooch.com/safe");
                }
            });
        }
        else
        {
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
                confirmButtonText: "Let's Go!",
                closeOnCancel: false,
                customClass: "securityAlert",
                allowEscapeKey: false,
                html: true
            }, function (isConfirm) {
                if (isConfirm)
                {
                    setTimeout(function () {
                        $('input#idVer-name').focus();
                    }, 500)
                }
                else
                {
                    window.open("https://www.nooch.com/safe");
                }
            });
        }

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
        transitionEffect: "slideLeft",
        transitionEffectSpeed: 400,
        titleTemplate: "#title#",

        /* Labels */
        labels: {
            finish: "Submit"
        },

        /* Events */
        onInit: function (event, currentIndex) {
            $('#idVerWiz > .content').animate({ height: "27em" }, 300)

            var DOB = $('#dob').val() ? $('#dob').val() : "1980 01 01";

            $('#idVer-dob').datetimepicker({
                format: 'MM/DD/YYYY',
                useCurrent: false,
                defaultDate: DOB,
                icons: {
                    previous: 'fa fa-fw fa-chevron-circle-left',
                    next: 'fa fa-fw fa-chevron-circle-right',
                    clear: 'fa fa-fw fa-trash-o'
                },
                maxDate: "1998-06-01",
                viewMode: 'years',
                //debug: true
            });

            var calendarIcon = $('.datePickerGrp i');

            calendarIcon.click(function ()
            {
                setTimeout(function ()
                {
                    $('#dobGrp .dtp-container.dropdown').addClass('fg-toggled open');
                    $('#idVer-dob').data("DateTimePicker").show();
                }, 150);
            });

            $('#idVer-ssn').mask("000 - 00 - 0000");
            $('#idVer-zip').mask("00000");
            $('#idVer-phone').mask('(000) 000-0000');

            $('[data-toggle="popover"]').popover();
        },
        onStepChanging: function (event, currentIndex, newIndex) {

            if (newIndex == 0) {
                $('#idVerWiz > .content').animate({ height: "26em" }, 600)
            }

            if (currentIndex == 1) {
                $('.createAccountPg #idVerWiz').css('overflow', 'hidden');
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

                        // Check Email field
                        $('#idVer-email').val($('#idVer-email').val().trim());

                        if (ValidateEmail($('#idVer-email').val()) == true)
                        {
                            updateValidationUi("email", true);

                            // Finally, check the phone number's length
                            if ($('#idVer-phone').cleanVal().length == 10)
                            {
                                updateValidationUi("phone", true);

                                // Great, we can finally go to the next step of the wizard :-D
                                $('#idVerWiz > .content').animate({ height: "19em" }, 600)
                                return true;
                            }
                            else {
                                updateValidationUi("phone", false);
                            }
                        }
                        else {
                            updateValidationUi("email", false);
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

                        $('#idVerWiz > .content').animate({ height: "26.5em" }, 500)
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

            // IF going to Step 4
            if (newIndex == 3)
            {
                // Check DOB field
                if ($('#idVer-dob').val().length == 10)
                {
                    // Double check that DOB is not still "01/01/1980", which is the default and probably not the user's B-Day...
                    if ($('#idVer-dob').val() != "01/01/1980") {
                        updateValidationUi("dob", true);

                        var ssnVal = $('#idVer-ssn').val().trim();
                        ssnVal = ssnVal.replace(/ /g, "").replace(/-/g, "");
                        // Check SSN field
                        if (ssnVal.length == 9 || FBID != "not connected") {
                            updateValidationUi("ssn", true);

                            // Great, go to the next step of the wizard :-]
                            // FILE INPUT DOCUMENTATION: http://plugins.krajee.com/file-input#options
                            $("#idVer_idDoc").fileinput({
                                allowedFileTypes: ['image'],
                                initialPreview: [
                                    "<img src='../Assets/Images/securityheader.png' class='file-preview-image' alt='' id='IdWizIdDocPreview'>"
                                ],
                                initialPreviewShowDelete: false,
                                layoutTemplates: {
                                    icon: '<span class="fa fa-photo m-r-10 kv-caption-icon"></span>',
                                },
                                fileActionSettings: {
                                    showZoom: false,
                                    indicatorNew: '',
                                },
                                maxFileCount: 1,
                                maxFileSize: 3000,
                                msgSizeTooLarge: "<strong>'{name}' ({size} KB)</strong> is a bit too large! Max allowed file size is <strong>{maxSize} KB</strong>. &nbsp;Please try a smaller picture!",
                                showCaption: false,
                                showUpload: false,
                                showPreview: true,
                                resizeImage: true,
                                maxImageWidth: 500,
                                maxImageHeight: 500,
                                resizePreference: 'width'
                            });

                            $('#idVer_idDoc').on('fileerror', function (event, data, msg) {
                                $('#idVerWiz > .content').animate({ height: "30em" }, 700)
                            });

                            $('#idVer_idDoc').on('fileloaded', function (event, file, previewId, index, reader) {
                                $('#idVerWiz > .content').animate({ height: "28em" }, 700)

                                isFileAdded = "1";
                                var readerN = new FileReader();

                                readerN.readAsDataURL(file);
                                readerN.onload = function (e) {
                                    // browser completed reading file - display it
                                    var splittable = e.target.result.split(',');
                                    var string2 = splittable[1];
                                    FileData = string2;
                                };
                            });

                            $('#idVer_idDoc').on('fileclear', function (event) {
                                $('#idVerWiz > .content').animate({ height: "22em" }, 800)
                                isFileAdded = "0";
                                FileData = null;
                            });

                            $('#idVer_idDoc').on('filecleared', function (event) {
                                $('#idVerWiz > .content').animate({ height: "22em" }, 800)
                                isFileAdded = "0";
                                FileData = null;
                            });

                            $('#idVerWiz > .content').animate({ height: "28em" }, 800)
                            return true;
                        }
                        else {
                            updateValidationUi("ssn", false);
                        }
                    }
                    else {
                        updateValidationUi("dob-default", false);
                    }
                }
                else {
                    updateValidationUi("dob", false);
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
            // Finish the Wizard...
            return true;
        },
        onFinished: function (event, currentIndex) {

            // SUBMIT DATA TO NOOCH SERVER
            console.log("New user, so calling createRecord()")
            createRecord();
        }
    });
}


function updateValidationUi(field, success) {
    //console.log("Field: " + field + "; success: " + success);

    if (field == "dob-default") {
        field = "dob";

        swal({
            title: "Forget Your Birthday?",
            text: "We hate identify fraud.  With a passion.<span class='show m-t-15'>So to help protect our users from the Bad Guys, " +
                  "please enter your <strong>date of birth</strong> to help verify your ID. &nbsp;Don't worry, this is never displayed anywhere and is only used to verify who you are.</span>" +
                  "<i class='fa fa-smile-o' style='font-size:36px; margin: 10px 0 0;'></i>",
            type: "warning",
            showCancelButton: false,
            confirmButtonColor: "#3fabe1",
            confirmButtonText: "Ok",
            html: true
        });
    }

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
            $('#idVerWiz > .content').animate({ height: "28em" }, 600)
            helpBlockTxt = isSmScrn ? "Please enter your <strong>SSN</strong>."
                                    : "<strong>Please enter your SSN.</strong>"// or connect with FB." // CC (6/7/16): Un-comment once Synapse finishes adding /user/docs/add to V3.0

            if (isSmScrn)
            {
                $('#idVerWiz > .content').animate({ height: "27em" }, 300)
            }
        }
        else if (field == "address") {
            helpBlockTxt = "Please enter <strong>just the <span style='text-decoration:underline;'>street address</span></strong> of where you <strong>currently</strong> live."
            $('#idVerWiz > .content').animate({ height: "21em" }, 600);
        }
        else if (field == "zip") {
            helpBlockTxt = "Please enter the ZIP code for the street address above."
            $('#idVerWiz > .content').animate({ height: "21em" }, 600);
        }
        else if (field == "email") {
            helpBlockTxt = "Please enter a valid email address that you own."
        }
        else if (field == "phone") {
            helpBlockTxt = "<strong>Please enter a valid 10-digit phone number.</strong>"
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
    console.log('createRecord fired');

    // ADD THE LOADING BOX
    $('#idWizContainer').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Validating Your Info...</span>',
        css: {
            border: 'none',
            padding: '26px 8px 20px',
            backgroundColor: '#000',
            '-webkit-border-radius': '15px',
            '-moz-border-radius': '15px',
            'border-radius': '15px',
            opacity: '.75',
            width: '270px',
            margin: '0 auto',
            color: '#fff'
        }
    });

    var transId = $('#transId').val();
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
    var cip = TYPE.length > 0 ? TYPE : "1";

    console.log("SAVE MEMBER INFO -> {memId: " + memId +
                                   ", Name: " + userNameVal +
                                   ", dob: " + dobVal +
                                   ", ssn: " + ssnVal +
                                   ", address: " + addressVal +
                                   ", zip: " + zipVal +
                                   ", email: " + userEmVal +
                                   ", phone: " + userPhVal +
                                   ", fngprnt: " + fngprntVal +
								   ", isIdImage: " + isFileAdded +
                                   ", IP: " + ipVal +
                                   ", isRS: " + RENTSCENE +
                                   ", Type: " + cip +
								   ", FBID: " + FBID + "}");

    $.ajax({
        type: "POST",
        url: "saveMemberInfo",
        data: "{  'transId':'" + transId +
             "', 'memId':'" + memId +
             "', 'name':'" + userNameVal +
             "', 'dob':'" + dobVal +
             "', 'ssn':'" + ssnVal +
             "', 'address':'" + addressVal +
             "', 'zip':'" + zipVal +
             "', 'email':'" + userEmVal +
             "', 'phone':'" + userPhVal +
             "', 'fngprnt':'" + fngprntVal +
             "', 'ip':'" + ipVal +
             "', 'pw':'" + '' +
             "', 'fbid':'" + FBID +
             "', 'rs':'" + RENTSCENE +
             "', 'cip':'" + cip +
             "', 'isIdImage':'" + isFileAdded +
             "', 'idImagedata':'" + FileData + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (result)
        {
            console.log("SUCCESS -> Save Member Info result is... [next line]");
            console.log(result);

            resultReason = result.reason;

            // Hide the Loading Block
            $('#idWizContainer').unblock();

            if (result.success == "true")
            {
                console.log("Success == true");

                memIdGen = result.memberIdGenerated;

                if (result.memberIdGenerated.length > 5)
                {
                    // Check if user's SSN verification was successful
                    if (result.ssn_verify_status != null &&
                        result.ssn_verify_status.indexOf("additional") > -1)
                    {
                        // Will need to send user to answer ID verification questions after selecting bank account
                        console.log("Need to answer ID verification questions");

                        $('#idWizContainer').addClass("animated bounceOut");

                        $("#idVerContainer iframe").attr("src", "https://www.noochme.com/noochweb/Nooch/idVerification?memid=" + memIdGen + "&from=lndngpg");
                        //$("#idVerContainer iframe").attr("src", "http://nooch.info/noochweb/Nooch/idVerification?memid=" + memIdGen + "&from=lndngpg");

                        // Show iFrame w/ ID Verification KBA Questions
                        setTimeout(function ()
                        {
                            $("#idVerContainer").addClass("bounceIn").removeClass("hidden");
                        }, 750);
                    }
                    else
                    {
                        // No KBA Questions returned, go straight to AddBank iFrame
                        idVerifiedSuccess();
                    }
                }
                else
                {
                    idVerifiedSuccess();
                }
            }
            else
            {
                console.log("Success != true");

                if (resultReason != null)
                {
                    console.log(resultReason);

					if (resultReason.indexOf('Validation PIN sent') > -1)
					{
						swal({
							title: "Check Your Phone",
							text: "To verify your phone number, we just sent a text message to your phone.  Please enter the <strong>PIN</strong> to continue.</span>" +
								  "<i class='show fa fa-mobile' style='font-size:40px; margin: 10px 0 0;'></i>",
							type: "input",
						    inputPlaceholder: "ENTER PIN",
							showCancelButton: true,
							confirmButtonColor: "#3fabe1",
							confirmButtonText: "Ok",
							customClass: "pinInput",
                            closeOnConfirm: false,
							html: true
						}, function (inputTxt) {
						    console.log("Entered Text: [" + inputTxt + "]");

						    if (inputTxt === false) return false;

						    if (inputTxt === "")
						    {
						        swal.showInputError("Please enter the PIN sent to your phone.");
						        return false
						    }
						    if (inputTxt.length < 4)
						    {
						        swal.showInputError("Double check you entered the entire PIN!");
						        return false
						    }

						    swal.close();

						    submitPin(inputTxt.trim())
						});
					}
                    else if (resultReason.indexOf("email already registered") > -1)
                    {
                        console.log("Error: email already registered");
                        showErrorAlert('20');
                    }
                    else if (resultReason.indexOf("phone number already registered") > -1)
                    {
                        console.log("Error: phone number already registered");
                        showErrorAlert('30');
                    }
                    else if (resultReason.indexOf("Missing critical data") > -1)
                    {
                        console.log("Error: missing critical data");
                        showErrorAlert('2');
                    }
                    else
                    {
                        showErrorAlert('2');
                    }
                }
                else
                {
                    console.log("Error checkpoint # 659");
                    showErrorAlert('2');
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


function idVerifiedSuccess()
{
    $(".errorMessage").addClass('hidden');

    $('#idWizContainer').slideUp()

    // Hide iFrame w/ ID Verification KBA Questions
    if (!$("#idVerContainer").hasClass("bounceOut"))
    {
        $("#idVerContainer").addClass("bounceOut");
    }

    setTimeout(function ()
    {
        // THEN DISPLAY SUCCESS ALERT...
        swal({
            title: "Submitted Successfully",
            text: "Thanks for submitting your ID information. That helps us keep " + COMPANY + " safe for everyone. &nbsp;We only use this information to prevent ID fraud and never share it without your permission." +
            "<span class='show m-t-10'>To finish, link any checking account:</span>" +
            "<span class=\"spanlist\"><span>1. &nbsp;Select your bank</span><span>2. &nbsp;Login with your regular online banking credentials</span><span>3. &nbsp;Choose which account to use</span></span>",
            type: "success",
            showCancelButton: false,
            confirmButtonColor: "#3fabe1",
            confirmButtonText: "Great!",
            closeOnConfirm: true,
            html: true,
            customClass: "idVerSuccessAlert",
        }, function () {

            console.log("memIDGen is: [" + memIdGen + "]");

            $("#AddBankDiv iframe").attr("src", $('#addBank_Url').val() + "?memberid=" + memIdGen + "&redUrl=createaccnt");

            $('#AddBankDiv').removeClass('hidden').addClass('bounceIn');

            setTimeout(function ()
            {
                scrollToAddBank();
            }, 800);
        });
    }, 500);
}


function submitPin(pin)
{
    console.log("submitPin fired - PIN [" + pin + "]");

    var memId = $('#memId').val();

    console.log("SubmitPIN Payload -> {memId: " + memId + ", PIN: " + pin + "}");

    // ADD THE LOADING BOX
    $('#idWizContainer').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Submitting PIN...</span>',
        css: {
            border: 'none',
            padding: '26px 8px 20px',
            backgroundColor: '#000',
            '-webkit-border-radius': '15px',
            '-moz-border-radius': '15px',
            'border-radius': '15px',
            opacity: '.75',
            'z-index': '99999',
            margin: '0 auto',
            color: '#fff'
        }
    });

    $.ajax({
        type: "POST",
        url: "submit2FAPin",
        data: "{ 'memberId':'" + memId +
             "', 'pin':'" + pin+ "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (result)
        {
            console.log("SUCCESS -> SubmitPIN result is... [next line]");
            console.log(result);

            resultReason = result.msg;

            // Hide the Loading Block
            $('#idWizContainer').unblock();

            if (result.success == true)
            {
                console.log("SubmitPIN: Success!");

                // THEN DISPLAY SUCCESS ALERT...
                swal({
                    title: "Great Success",
                    text: "Your phone number has been confirmed.",
                    type: "success",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Continue",
                    closeOnConfirm: true,
                    html: true,
                    customClass: "idVerSuccessAlert",
                }, function (isConfirm)
                {
                    // SUBMIT ID WIZARD DATA TO SERVER AGAIN...
                    console.log("Calling createRecord() again...")
                    createRecord();
                });
            }
            else
            {
                console.log("Success != true");

                if (resultReason != null)
                {
                    console.log(resultReason);

                    if (resultReason.indexOf('Validation PIN sent') > -1)
                    {
                        swal({
                            title: "Check Your Phone",
                            text: "To verify your phone number, we just sent a text message to your phone.  Please enter the <strong>PIN</strong> to continue.</span>" +
								  "<i class='show fa fa-mobile' style='font-size:40px; margin: 10px 0 0;'></i>",
                            type: "input",
                            showCancelButton: true,
                            confirmButtonColor: "#3fabe1",
                            confirmButtonText: "Ok",
                            html: true
                        }, function (inputTxt)
                        {
                            console.log("Entered Text: [" + inputTxt + "]");
                        });
                    }
                    else if (resultReason.indexOf("email already registered") > -1) {
                        console.log("Error: email already registered");
                        showErrorAlert('20');
                    }
                    else if (resultReason.indexOf("phone number already registered") > -1) {
                        console.log("Error: phone number already registered");
                        showErrorAlert('30');
                    }
                    else if (resultReason.indexOf("Missing critical data") > -1) {
                        console.log("Error: missing critical data");
                        showErrorAlert('2');
                    }
                    else {
                        showErrorAlert('2');
                    }
                }
                else {
                    console.log("Error checkpoint [#834]");
                    showErrorAlert('2');
                }
            }
        },
        Error: function (x, e)
        {
            // Hide the Loading Block
            $('#idWizContainer').unblock();

            console.log("Submit PIN ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('3');
        }
    });
}


// To handle success from extra verification iFrame
$('body').bind('complete', function ()
{
    var result = $('#iframeIdVer').get(0).contentWindow.isCompleted;

    // Hide the Loading Block
    $('#idVerContainer').unblock();

    console.log("Callback from ID Quest - success was: [" + result + "]");

    if (result == true)
    {
        idVerifiedSuccess();
    }
    else
    {
        // Hide the ID Questions iFrame
        $("#idVerContainer").addClass("hidden");

        // Show error msg
        {
            showErrorAlert('25');
        }
    }
});

$('body').bind('addblockLdg', function ()
{
    // Show the Loading Block
    $('#idVerContainer').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Submitting responses...</span>',
        css: {
            border: 'none',
            padding: '26px 8px 20px',
            backgroundColor: '#000',
            '-webkit-border-radius': '15px',
            '-moz-border-radius': '15px',
            'border-radius': '15px',
            opacity: '.75',
            width: '270px',
            margin: '0 auto',
            color: '#fff'
        }
    });
});

// To handle success from Add-Bank page (CC: 1/20/16)
// CC (5/24/16): NEED TO UPDATE FOR NEW ARCHITECTURE
$('body').bind('addBankComplete', function ()
{
    $('#AddBankDiv').slideUp();

    setTimeout(function ()
    {
        // THEN DISPLAY SUCCESS ALERT...
        swal({
            title: "Bank Linked Successfully",
            text: "<i class=\"fa fa-bank text-success\"></i>" +
                  "<span class='show m-t-10'>Thanks for completing this <strong>one-time</strong> process. &nbsp;Now you can make secure payments with anyone and never share your bank details!</span>",
            type: "success",
            confirmButtonColor: "#3fabe1",
            confirmButtonText: "Done",
            customClass: "largeText",
            html: true
        });

        if (ISNEW == "true")
        {
            $('#checkEmailMsg').removeClass('hidden');
        }
        else
        {
            $('.resultDiv').removeClass('hidden');
        }

    }, 500);


    // OLD CODE FOR LETTING USER ADD A PW - NEED TO UPDATE FOR V3.0

  /*swal({
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
                padding: '26px 8px 20px',
                backgroundColor: '#000',
                '-webkit-border-radius': '15px',
                '-moz-border-radius': '15px',
                'border-radius': '15px',
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
                var setPw = msg;
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
    });*/
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

    var supportEmail = "support@nooch.com";
    if (COMPANY == "Rent Scene") {
        supportEmail = "payments@rentscene.com"
    }

    console.log("ShowError -> errorNum is: [" + errorNum + "], resultReason is: [" + resultReason + "]");

    if (errorNum == '1') // Codebehind errors
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "We had trouble finding that transaction.  Please try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
						COMPANY + " Support</span>:<br/><a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
    }
    else if (errorNum == '2') // Errors after submitting ID verification AJAX
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing your info.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
                        COMPANY + " Support</span>:<br/><a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
    }
    else if (errorNum == '25') // Errors from the iFrame with the multiple choice verification questions
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing your info.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
                        COMPANY + " Support</span>:<br/><a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
    }
    else if (errorNum == '20') // Submitted ID Verification info, but EMAIL came back as already registered with Nooch.
    {
        alertTitle = "Email Already Registered";
        alertBodyText = "Looks like <strong>" + $('#idVer-email').val() + "</strong> is already registered to a " + COMPANY + " account.  Please try a different email address.";
        shouldFocusOnEmail = true;
        shouldShowErrorDiv = false;
    }
    else if (errorNum == '30') // Submitted ID Verification info, but PHONE came back as already registered with Nooch.
    {
        alertTitle = "Phone Number Already Registered";
        alertBodyText = "Looks like <strong>" + $('#idVer-phone').val() + "</strong> is already registered to a " + COMPANY + " account.  Please try a different number.";
        shouldFocusOnPhone = true;
        shouldShowErrorDiv = false;
    }
    else // Generic Error
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing your request.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
                        COMPANY + " Support</span>:<br/><a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
    }

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
        confirmButtonText: "Ok",
        cancelButtonText: "Contact Support",
        closeOnConfirm: true,
        closeOnCancel: false,
        allowEscapeKey: false,
        html: true
    }, function (isConfirm) {
        if (!isConfirm) {
            window.open("mailto:" + supportEmail);
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

function ssnWhy()
{
    swal({
        title: "Why Do We Collect SSN?",
        text: "We hate identify fraud.  With a passion.<span class='show m-t-15'>" +
              "In order to keep " + COMPANY + " safe for all users, and to comply with federal and state measures against identity theft, we use your SSN for one purpose only: verifying your ID. &nbsp;Your SSN is never displayed anywhere and is only transmitted with bank-grade encryption.</span>" +
              "<span class='show'><a href='https://en.wikipedia.org/wiki/Know_your_customer' class='btn btn-link p-5 f-16' target='_blank'>Learn More<i class='fa fa-external-link m-l-10 f-15'></i></a></span>",
        type: "info",
        showCancelButton: false,
        confirmButtonColor: "#3fabe1",
        confirmButtonText: "Ok",
        html: true
    });
}


function changeFavicon(src) {
  var link = document.createElement('link'),
   oldLink = document.getElementById('dynamic-favicon');
  link.id = 'dynamic-favicon';
  link.rel = 'shortcut icon';
  link.href = src;
  if (oldLink) {
    document.head.removeChild(oldLink);
  }
  document.head.appendChild(link);
}

// -------------------
//	Scroll To Section
// -------------------
function scrollToAddBank()
{
    var scroll_to = $('#frame').offset().top - 75;

    if ($(window).scrollTop() != scroll_to) {
        $('html, body').stop().animate({ scrollTop: scroll_to }, 1000, null);
    }
}

// -------------------
//	FACEBOOK
// -------------------
window.fbAsyncInit = function ()
{
    FB.init({
        appId: '198279616971457',
        xfbml: true,
        version: 'v2.6'
    });
};

(function (d, s, id) {
    var js, fjs = d.getElementsByTagName(s)[0];
    if (d.getElementById(id)) { return; }
    js = d.createElement(s); js.id = id;
    js.src = "//connect.facebook.net/en_US/sdk.js";
    fjs.parentNode.insertBefore(js, fjs);
}(document, 'script', 'facebook-jssdk'));

var fbStatus = "";
// The response object is returned with a status field that lets the app know the current login status of the person.
function checkLoginState() {
    
    FB.getLoginStatus(function (response) {
       
        console.log(response);
        fbStatus = response.status;

        if (response.status === 'connected') {
            // Logged into your app and FB
            FBID = response.authResponse.userID;
            $('#fbLoginBtn span').text('Facebook Connected');
            $('#fbResult').html('<strong>Facebook Connected Successfully! <i class="fa fa-smile-o m-l-5"></i></strong>')
						  .addClass('bounceIn text-success').removeClass('hidden');
        }
        else {
            // Not logged in, so attempt to Login to FB
            fbLogin();
        }
    });
}

function fbLogin() {
    
    FB.login(function (response) {
        if (response.status === 'connected') {
            FBID = response.authResponse.userID;

            $('#fbLoginBtn span').text('Facebook Connected');
            $('#fbResult').html('<strong>Facebook Connected Successfully! <i class="fa fa-smile-o m-l-5"></i></strong>')
						  .addClass('bounceIn text-success').removeClass('hidden');
        }
        else {
            FBID = "not connected";
        }
    });
}
