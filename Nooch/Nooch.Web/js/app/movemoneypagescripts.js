var transType = $('#transType').val();
var transStatus = $("#pymnt_status").val();
var TRANSID = $('#transId').val();
var USERTYPE = $('#usrTyp').val();
var MemID_EXISTING = $('#memidexst').val();
var FOR_RENTSCENE = $('#rs').val();

var usrPhn, usrEm;
var resultReason = "";
var fingprint = "";
var memIdGen = "";
var completedFirstWizard = false;
var sendToXtraVer = false;

var isSmScrn = false;
var isLrgScrn = false;

$(document).ready(function () {
    // For large scrns, animate payment info to left side to be visible under the ID Ver Modal
    if ($(window).width() < 768) {
        isSmScrn = true;
    }
    else if ($(window).width() > 1000) {
        isLrgScrn = true;

        if (USERTYPE != "NonRegistered" && USERTYPE != "Existing")
        {
            var targetWdth = '30%';
            var ms = 1250;
            if ($(window).width() > 1100) {
                targetWdth = '33%';
                if ($(window).width() > 1200) {
                    targetWdth = '34%';
                    ms = 1000;
                }
            }
            $("#payreqInfo").animate({
                width: targetWdth
            }, ms, 'easeOutQuart');
        }
    }

    var verb = (transType == "send") ? "get paid" : "pay anyone";

    var alertBodyOpening = "Nooch is a quick, secure way to " + verb + " without having to enter a credit card.";

    if (FOR_RENTSCENE == "true")
    {
        alertBodyOpening = "Rent Scene offers a quick, secure way to " + verb + " without having to enter a credit card.";
        $('.landingHeaderLogo').attr('href', 'http://www.rentscene.com');
        $('.landingHeaderLogo img').attr('src', '../Assets/Images/rentscene.png');
        $('.landingHeaderLogo img').attr('alt', 'Rent Scene Logo');
        if (isLrgScrn)
            $('.landingHeaderLogo img').css('width', '211px');
    }


    if (areThereErrors() == false)
    {
        
        if (checkIfStillPending() == true)
        {
            //console.log(checkIfStillPending());
            
            console.log("UserType is: [" + USERTYPE + "]");

            if (USERTYPE != "NonRegistered" &&
                USERTYPE != "Existing") // Only other option is "New"
            {
                 
                setTimeout(bindEmail, 700);

                // Hide old form
                $('#PayorInitialInfo').addClass('hidden');

                var confirmBtnTxt = "Great, Let's Go!";
                if (isSmScrn) {
                    confirmBtnTxt = "Let's Go!"
                }
                swal({
                    title: "Secure, Private Payments",
                    text: "<p>" + alertBodyOpening +
                          " &nbsp;Just verify your ID, then login to your online banking<span class='desk-only'> as you normally do</span>.</p>" +
                          "<ul class='fa-ul'><li><i class='fa-li fa fa-check'></i>We don't store your bank credentials</li>" +
                          "<li><i class='fa-li fa fa-check'></i>Nobody ever sees any of your personal or bank info (except your name)</li>" +
                          "<li><i class='fa-li fa fa-check'></i>We use <strong>bank-grade encryption</strong> to secure all data</li></ul>",
                    imageUrl: "../Assets/Images/secure.svg",
                    imageSize: "194x80",
                    showCancelButton: true,
                    cancelButtonText: "Learn More",
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: confirmBtnTxt,
                    closeOnCancel: false,
                    customClass: "securityAlert",
                    allowEscapeKey: false,
                    html: true
                }, function (isConfirm) {
                    if (isConfirm) {
                        setTimeout(function () {
                            //Get Fingerprint
                            new Fingerprint2().get(function (result) {
                                fingprint = result;
                            });

                            runIdWizard()

                        }, 250);
                    }
                    else {
                        window.open("https://www.nooch.com/safe");
                    }
                    //console.log(ipusr);
                });
            }
            else if (USERTYPE == "NonRegistered" && 
                     $('#bnkName').val() == "no bank found")
            {
                
                swal({
                    title: "Secure, Private Payments",
                    text: "To complete this payment, you can securely link any US checking account using your online banking account:" +
                           "<ul class='fa-ul'><li><i class='fa-li fa fa-check'></i>We don't store your bank credentials</li>" +
                          "<li><i class='fa-li fa fa-check'></i>Nobody ever sees any of your personal or bank info (except your name)</li>" +
                          "<li><i class='fa-li fa fa-check'></i>We use <strong>bank-grade encryption</strong> to secure all data</li></ul>",
                    imageUrl: "../Assets/Images/secure.svg",
                    imageSize: "194x80",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Continue",
                    html: true,
                    customClass: "securityAlert confirmBtnFullWidth"
                }, function () {
                    redUrlForAddBank = (transType == "send") ? "https://www.noochme.com/noochweb/nooch/depositMoneycomplete?mem_id="
                                                             : "https://www.noochme.com/noochweb/nooch/payRequestComplete?mem_id=";

                    redUrlForAddBank = redUrlForAddBank + MemID_EXISTING + "," + TRANSID;

                    redUrlForAddBank = (FOR_RENTSCENE == "true") ? redUrlForAddBank + ",true"
                                                                 : redUrlForAddBank + ",false";


                    console.log("redUrlForAddBank IS: [" + redUrlForAddBank + "]");

                     //$("#frame").attr("src", "https://www.noochme.com/noochweb/trans/Add-Bank.aspx?MemberId=" + MemID_EXISTING +
                     //                      "&redUrl=" + redUrlForAddBank);

                    $("#frame").attr("src",$('#addBank_Url').val()+  "?memberid=" + MemID_EXISTING +
                                          "&redUrl=" + redUrlForAddBank);
                    $('#AddBankDiv').removeClass('hidden').addClass('bounceIn');
                });
            }

            $('#idVer .modalclose').click(function () {
                cancelIdVer();
            });

            $('#relaunchIdwiz > button').click(function () {
                //Get Fingerprint Again
                new Fingerprint2().get(function (result) {
                    fingprint = result;
                });

                $('#relaunchIdwiz').removeClass('bounceIn').addClass('bounceOut');

                if (completedFirstWizard == true)
                {
                    $('#idVer').modal({
                        backdrop: 'static',
                        keyboard: false
                    });
                }
                else
                {
                    runIdWizard();
                }
            });
        }
    }
    else {
        console.log("114. There was an error! :-(");
    }

    // Format the Memo if present
    if ($("#transMemo").text().length > 0) {
        $("#transMemo").prepend("<i class='fa fa-fw fa-commenting fa-flip-horizontal'>&nbsp;</i><em>&quot;</em>").append("<em>&quot;</em>");
    }
});


function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);
    return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}


function runIdWizard() {idVer
    $('#idVer').modal({
        backdrop: 'static',
        keyboard: false
    });

    $('#idVer').on('shown.bs.modal', function (e) {
        if (isLrgScrn == true) {
            $("#idVer .modal-dialog").animate({
                'margin-left': '48%'
            }, 750, 'easeInOutCubic', function () {
                setTimeout(function () {
                    $('input#idVer-name').focus();
                }, 200)
            });
            $("#payreqInfo").animate({
                width: '40%',
                left: '5%'
            }, 750, 'easeInOutQuad');
        }
    });

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
            var heightToUse = isSmScrn ? "23em" : "24em";

            $('#idVerWiz > .content').animate({ height: heightToUse }, 300)

            $('[data-toggle="popover"]').popover();

            var calendarIcon = $('#idVerForm1 .datePickerGrp i');

            calendarIcon.click(function ()
            {
                //console.log("FOCUS REACHED!");

                setTimeout(function ()
                {
                    $('#dobGrp .dtp-container.dropdown').addClass('fg-toggled open');
                    $('#idVer-dob').data("DateTimePicker").show();
                }, 150);
            });

            $('#idVer-dob').datetimepicker({
                format: 'MM/DD/YYYY',
                useCurrent: false,
                defaultDate: moment("1980 01 01", "YYYY MM DD"),
                icons: {
                    previous: 'fa fa-fw fa-chevron-circle-left',
                    next: 'fa fa-fw fa-chevron-circle-right',
                    clear: 'fa fa-fw fa-trash-o'
                },
                maxDate: moment("1996 12 31", "MYYYY MM DD"),
                viewMode: 'years',
                //debug: true
            });

            $('#idVer-ssn').mask("0000");
            $('#idVer-zip').mask("00000");
            $('#idVer-phone').val(usrPhn);
            $('#idVer-email').val(usrEm);
            $('#idVer-phone').mask('(000) 000-0000');
        },
        onStepChanging: function (event, currentIndex, newIndex) {

            if (newIndex == 0) {
                $('#idVerWiz > .content').animate({ height: "23em" }, 600)
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
                                $('#idVerWiz > .content').animate({ height: "19em" }, 600)
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

                        $('#idVerWiz > .content').animate({ height: "19em" }, 500)
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
            if (currentIndex == 1)
            {
                $('#idVer-address').focus();
            }
            else if (currentIndex == 2)
            {
                $('#idVer-email').focus();
            }
        },
        onCanceled: function (event) {
            cancelIdVer();
        },
        onFinishing: function (event, currentIndex) {
            // CHECK TO MAKE SURE ALL FIELDS WERE COMPLETED

            $('#idVer-email').val($('#idVer-email').val().trim());

            if (ValidateEmail($('#idVer-email').val()) == true)
            {
                updateValidationUi("email", true);

                // Finally, check the phone number's length
                console.log($('#idVer-phone').cleanVal());

                if ($('#idVer-phone').cleanVal().length == 10)
                {
                    updateValidationUi("phone", true);

                    // Finish the Wizard...
                    return true;
                }
                else
                {
                    updateValidationUi("phone", false);
                }
            }
            else
            {
                updateValidationUi("email", false);
            }
        },
        onFinished: function (event, currentIndex) {

            // ADD THE LOADING BOX
            showLoadingBox(1);

            // SUBMIT DATA TO NOOCH SERVER
            createRecord();
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
            helpBlockTxt = isSmScrn ? "Please enter just the <strong>last 4</strong> digits of your SSN."
                                    : "Please enter just the <strong>LAST 4 digits</strong> of your SSN. This is used solely to protect your account."

            if (isSmScrn)
            {
                $('#idVerWiz > .content').animate({ height: "24em" }, 300)
            }
        }
        else if (field == "address") {
            helpBlockTxt = "Please enter the physical street address of where you <strong>currently</strong> live."
        }
        else if (field == "zip") {
            helpBlockTxt = "Please enter the zip code for the street address above."
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


function cancelIdVer() {
    var alertText = "";
    if (transType == "send")
    {
        alertText = "Are you sure you want to cancel?  You must complete this step before you can accept this payment.  It will take less than 60 seconds, and we never share your data with anyone.  Period.";
    }
    else // must be a Request OR Rent payment
    {
        alertText = "Are you sure you want to cancel?  You must complete this step before you can pay this request.  It will take less than 60 seconds, and we never share your data with anyone.  Period.";
    }

    swal({
        title: "Cancel ID Verification?",
        text: alertText,
        type: "warning",
        showCancelButton: true,
        confirmButtonColor: "#DD6B55",
        cancelButtonColor: "#dd1e00",
        confirmButtonText: "No - Complete Now",
        cancelButtonText: "Yes - Cancel",
        closeOnConfirm: true,
        closeOnCancel: true,
        allowEscapeKey: false
    }, function (isConfirm) {
        if (!isConfirm) {
            $('#idVer').modal('hide');

            $('#idVer').on('hidden.bs.modal', function (e) {
                if (isLrgScrn == true) {
                    $("#payreqInfo").animate({
                        width: '100%',
                        left: '0%'
                    }, 700, 'easeInOutCubic', function () {
                        $('#relaunchIdwiz').removeClass('hidden').removeClass('bounceOut').addClass('bounceIn');
                    });
                }
            });

            // Show the button to re-launch the ID Wizard
            setTimeout(function () {
                $('#idVerWiz').steps('destroy');
            }, 250);
        }
    });
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

    console.log("{transId: " + TRANSID + ", userEm: " + userEmVal + ", userPh: " + userPhVal + ", userName: " + userNameVal +
                ", userPw: " + userPwVal + ", ssn: " + ssnVal + ", dob: " + dobVal + ", fngprnt: " + fngprntVal + ", ip: " + ipVal + "}");

    var urlToUse = "";
    if (transType == "send")
    {
        urlToUse = "depositMoney.aspx/RegisterUserWithSynp";
    }
    else // must be a Request or Rent payment (which also uses the payRequest page)
    {
        urlToUse = "RegisterUserWithSynp";
    }
    //console.log("URL to use: " + urlToUse);

    var dataToSend = "";

    dataToSend = "{'transId':'" + TRANSID +
                 "', 'memberId':'" + MemID_EXISTING +
                 "', 'userEm':'" + userEmVal +
                 "', 'userPh':'" + userPhVal +
                 "', 'userName':'" + userNameVal +
                 "', 'userPw':'" + userPwVal +
                 "', 'ssn':'" + ssnVal +
                 "', 'dob':'" + dobVal +
                 "', 'address':'" + addressVal +
                 "', 'zip':'" + zipVal +
                 "', 'fngprnt':'" + fngprntVal +
                 "', 'ip':'" + ipVal + "'}"
    
    console.log(dataToSend);
    
    $.ajax({
        type: "POST",
        url: urlToUse,
        data: dataToSend,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {

            var RegisterUserWithSynpResult = msg;
            console.log("SUCCESS -> 'RegisterUserWithSynpResult' is... ");
            console.log(RegisterUserWithSynpResult);

			resultReason = RegisterUserWithSynpResult.reason;

            // Hide the Loading Block
			$('.modal-content').unblock();

			if (RegisterUserWithSynpResult.success == "true" &&
			    RegisterUserWithSynpResult.memberIdGenerated.length > 5)
			{
			    memIdGen = RegisterUserWithSynpResult.memberIdGenerated;

			    // Check if user's SSN verification was successful
			    if (RegisterUserWithSynpResult.ssn_verify_status != null &&
                    RegisterUserWithSynpResult.ssn_verify_status.indexOf("additional") > -1)
			    {
			        // Will need to send user to answer ID verification questions after selecting bank account
			        console.log("Need to answer ID verification questions");

			        var g = true;
			        if (g == true)
			        {
			            sendToXtraVer = true;
			            completedFirstWizard = true;

			            $("#idVerWiz").addClass("animated bounceOut");

			            $("#idVerContainer iframe").attr("src", "https://www.noochme.com/noochweb/trans/idverification.aspx?memid=" + memIdGen + "&from=lndngpg");

			            setTimeout(function () {
			                $("#idVerWiz").css({
			                    "height": "0",
			                    "padding": "0"
			                });
			                $("#idVer .modal-body").css("padding-top", "0");
			                $("#idVerContainer").addClass("bounceIn").removeClass("hidden");
			            }, 1000);
			        }
			        else
			        {
			            idVerifiedSuccess();
			        }
			    }

			    else // No ID questions needed
			    {
			        idVerifiedSuccess();
			    }
            }
			else 
			{
			    console.log("RegisterUserWithSynpResult.success = false");
			    if (resultReason != null)
			    {
			        console.log(resultReason);

			        if (resultReason.indexOf("email already registered") > -1)
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
			        showErrorAlert('2');
			    }
            }
            
        },
        Error: function (x, e) {
            // Hide the Loading Block
            $('.modal-content').unblock();
            console.log("ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('2');
        }
    });
}


function idVerifiedSuccess() {
    // HIDE THE MODAL CONTAINING THE WIZARD
    $('#idVer').modal('hide');

    $('#idVer').on('hidden.bs.modal', function (e) {
        if (isLrgScrn == true) {
            $("#payreqInfo").animate({
                width: '100%',
                left: '0%'
            }, 1000, 'easeInOutQuad');
        }
    });

    // THEN DISPLAY SUCCESS ALERT...
    setTimeout(function () {
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
            customClass: "idVerSuccessAlert"
        }, function () {
            
            redUrlForAddBank = (transType == "send") ? "https://www.noochme.com/noochweb/nooch/depositMoneycomplete?mem_id="
                                                     : "https://www.noochme.com/noochweb/nooch/payRequestComplete?mem_id=";
            
            redUrlForAddBank = redUrlForAddBank + memIdGen + "," + TRANSID;

            redUrlForAddBank = (FOR_RENTSCENE == "true") ? redUrlForAddBank + ",true"
                                                         : redUrlForAddBank + ",false";


            console.log("redUrlForAddBank IS: [" + redUrlForAddBank + "]");

            //$("#frame").attr("src", "Nooch/Add-Bank.aspx?MemberId=" + memIdGen +
            //                        "&redUrl=" + redUrlForAddBank);

            $("#frame").attr("src", $('#addBank_Url').val()+ "?memberid=" + memIdGen +
                                          "&redUrl=" + redUrlForAddBank);

            $('#AddBankDiv').removeClass('hidden').addClass('bounceIn');
        });
    }, 500);
}


function areThereErrors() {

    if (errorFromCodeBehind != '0')
    {
        console.log('areThereErrors -> errorFromCodeBehind is: [' + errorFromCodeBehind + "]");

        // Position the footer absolutely so it's at the bottom of the screen (it's normally pushed down by the body content)
        $('.footer').css({
            position: 'fixed',
            bottom: '5%'
        })

        showErrorAlert(errorFromCodeBehind);

        return true;
    }

    console.log("No Errors!");
    return false;
}


function showLoadingBox(n) {
    var msg = "";
    if (n == 1) {
        msg = "Validating Your Info...";
    }
    else if (n == 2){
        msg = "Saving Password...";
    }
    else {
        msg = "Submitting responses...";
    }

    $('.modal-content').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">' + msg + '</span>',
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
	else if (errorNum == '2') // Errors after submitting ID verification AJAX
	{
	    alertTitle = "Errors Are The Worst!";
	    alertBodyText = "Terrible sorry, but it looks like we had trouble processing your info.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
	}
	else if (errorNum == '25') // Errors from the iFrame with the multiple choice verification questions
	{
	    alertTitle = "Errors Are The Worst!";
	    alertBodyText = "Terrible sorry, but it looks like we had trouble processing your info.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
	}
	else if (errorNum == '20') // Submitted ID Verification info, but EMAIL came back as already registered with Nooch.
	{
	    alertTitle = "Email Already Registered";
	    alertBodyText = "Looks like <strong>" + $('#idVer-email').val() + "</strong> is already registered to a Nooch account.  Please try a different email address.";
	    shouldFocusOnEmail = true;
	    shouldShowErrorDiv = false;
	}
	else if (errorNum == '30') // Submitted ID Verification info, but PHONE came back as already registered with Nooch.
	{
	    alertTitle = "Phone Number Already Registered";
	    alertBodyText = "Looks like <strong>" + $('#idVer-phone').val() + "</strong> is already registered to a Nooch account.  Please try a different number.";
	    shouldFocusOnPhone = true;
	    shouldShowErrorDiv = false;
	}
	else // Generic Error
	{
	    alertTitle = "Errors Are The Worst!";
	    alertBodyText = "Terrible sorry, but it looks like we had trouble processing your request.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
	}

	if (shouldShowErrorDiv == true)
	{
	    $(".errorMessage").removeClass('hidden');
	    $(".errorMessage").html(alertBodyText);
	    $(".errorMessage a").addClass("btn btn-default m-t-20 animated bounceIn");
	}
	else
	{
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


function checkIfStillPending() 
{
    if (transStatus == "pending") // Set on Code Behind page
    {
        return true;
    }
    else
    {
        var alertTitle = "";
        var alertBodyText = "";

        if (transType == "send")
        {
            if (transStatus == "success") {
                alertTitle = "Payment Already Completed";
                alertBodyText = "Looks like this payment has already been accepted successfully.";
            }
            else if (transStatus == "cancelled") {
                alertTitle = "Payment Already Cancelled";
                alertBodyText = "Looks like " + $('#senderName1').text() + " already cancelled this payment before you could accept it.&nbsp; Sorry about that!";
            }
            else if (transStatus == "rejected") {
                alertTitle = "Payment Already Rejected";
                alertBodyText = "Looks like you already rejected this payment, unfortunately.";
            }
            else {
                alertTitle = "Payment Expired";
                alertBodyText = "Looks like this payment request is no longer pending.&nbsp; You're off the hook!";
            }
        }
        else if (transType == "rent")
        {
            if (transStatus == "success") {
                alertTitle = "Already Paid";
                alertBodyText = "Looks like this payment request was already paid successfully.";
            }
            else if (transStatus == "cancelled") {
                alertTitle = "Payment Request Already Cancelled";
                alertBodyText = "Looks like " + $('#senderName1').text() + " has cancelled this payment.";
            }
            else if (transStatus == "rejected") {
                alertTitle = "Request Already Rejected";
                alertBodyText = "Looks like you already rejected this payment. &nbsp;" + $('#senderName1').text() + " will need to send a new payment request.";
            }
            else {
                alertTitle = "Request Expired";
                alertBodyText = "Looks like this payment request is no longer pending.";
            }
        }
        else // must be a "request" transfer type
        {
            if (transStatus == "success")
            {
                alertTitle = "Request Already Paid";
                alertBodyText = "Looks like this payment request was already paid successfully.";
            }
            else if (transStatus == "cancelled")
            {
                alertTitle = "Request Already Cancelled";
                alertBodyText = "Looks like " + $('#senderName1').text() + " already cancelled this payment.&nbsp; You're off the hook!";
            }
            else if (transStatus == "rejected")
            {
                alertTitle = "Request Already Rejected";
                alertBodyText = "Looks like you already rejected this payment.";
            }
            else
            {
                alertTitle = "Request Expired";
                alertBodyText = "Looks like this payment request is no longer pending.&nbsp; You're off the hook!";
            }
        }
            
        
        alertBodyText += "&nbsp;&nbsp;Please contact <span style='font-weight:600;'>Nooch Support</span> if you believe this is an error.<br/>" +
                         "<a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";

        $(".errorMessage").html(alertBodyText).removeClass('hidden');
        $(".errorMessage a").addClass("btn btn-default m-t-20 animated bounceIn");

        swal({
            title: alertTitle,
            text: alertBodyText,
            type: "error",
            confirmButtonColor: "#3fabe1",
            confirmButtonText: "Ok",
            closeOnConfirm: true,
            allowEscapeKey: false,
            html: true
        });

        return false;
    }
}


function bindEmail()
{
    if ($('#invitationType').val() == "p")
    {
        usrEm = "";
        usrPhn = $('#invitationSentto').val().trim();
    }
    else
    {
        usrEm = $('#invitationSentto').val().trim();
        usrPhn = "";
    }
}


// To handle success from extra verification iFrame
$('body').bind('complete', function () {
    var result = $('#iframeIdVer').get(0).contentWindow.isCompleted;

    // Hide the Loading Block
    $('.modal-content').unblock();

    console.log("Callback from ID Quest - success was: [" + result + "]");

    if (result == true)
    {
        idVerifiedSuccess();
    }
    else
    {
        // Hide the ID Questions iFrame
        $("#idVerContainer").addClass("hidden");
        $('#idVer').modal('hide');

        // Show error msg
        {
            showErrorAlert('25');
        }
    }
});
$('body').bind('addblockLdg', function () {
    // Show the Loading Block
    showLoadingBox(3);
});

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

/*  For Existing but NonRegistered Users  */
function payBtnClicked()
{
    var senderName = $('#senderName1').text();
    var amount = $('#transAmountd').text().trim() + "." + $('#cents > span').text().trim();
    var bnkName = $('#bnkName').val();
    var bnkNicknam = $('#bnkNickname').val();

    var msg = "You are about to make a <strong>$" + amount + "</strong> payment to <strong>" + senderName + "</strong>";

    if (bnkNicknam == "manual") // This flag is sent by the user when the bank has a blank 'bank_name' b/c it was added via routing/account #
    {
        msg += " using your bank account ending in:" +
              "<span class='show m-t-10'><strong>" + bnkName + "</strong></span>";
    }
    else
    {
        msg += " using the following bank account:" +
              "<span class='show m-t-15'><strong>" + bnkName + "</strong> &nbsp;" + bnkNicknam + "</span>";
    }

    msg += "<span class='show m-t-15'>By clicking confirm, you are authorizing a one-time transfer from your bank account to " + senderName + ".</span>"
    

    swal({
        title: "Confirm Payment To " + senderName,
        text: msg,
        imageUrl: $('#senderImage').attr('src'),
        imageSize: "100x100",
        showCancelButton: true,
        confirmButtonColor: "#3fabe1",
        confirmButtonText: " Confirm",
        closeOnConfirm: true,
        html: true,
        customClass: "nonRegisteredUserPayConfirm"
    }, function (isConfirm) {
        if (isConfirm)
        {
            var redUrlToSendTo = "";

            redUrlToSendTo = (transType == "send") ? "https://www.noochme.com/noochweb/trans/depositMoneycomplete.aspx?mem_id="
                                                     : "https://www.noochme.com/noochweb/trans/payRequestComplete.aspx?mem_id=";

            redUrlToSendTo = redUrlToSendTo + MemID_EXISTING + "," + TRANSID;

            redUrlToSendTo = (FOR_RENTSCENE == "true") ? redUrlToSendTo + ",true"
                                                       : redUrlToSendTo + ",false";

            console.log("redUrlForAddBank IS: [" + redUrlToSendTo + "]");

            window.location = redUrlToSendTo;
        }
    });
}

function rejectBtnClicked() {
    var userTypeEncr = "";

    if (USERTYPE === "NonRegistered")
    {
        userTypeEncr = "6KX3VJv3YvoyK+cemdsvMA==";
    }
    else if (USERTYPE === "Existing")
    {
        userTypeEncr = "mx5bTcAYyiOf9I5Py9TiLw==";
    }

    if (transType == "send")
    {
        //window.location = "https://www.noochme.com/noochweb/trans/depositMoneycomplete.aspx?mem_id=" + MemID_EXISTING + "," + ;
    }
    else // must be a request
    {
        window.location = "https://www.noochme.com/noochweb/trans/rejectMoney.aspx?TransactionId=" + TRANSID +
                          "&UserType=" + userTypeEncr +
                          "&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==" +
                          "&TransType=T3EMY1WWZ9IscHIj3dbcNw==";
    }
}


// -----------------
// UNUSED FUNCTIONS
// -----------------
/*
// CLIFF (9/25/15): CAN DELETE.
// Submit User Profile Info Form
$('form#PayorInitialInfoForm').submit(function (e) {
    e.preventDefault();
    checkFields123();
});

// CLIFF (9/25/15): CAN DELETE.
// Submit User PW Form
$('form#crtPwFrm').submit(function (e) {
    e.preventDefault();
    checkPwForm();
});

function checkFields123() {
    var id = "";

    // First validate the 'User Name' input
    var fullName = $('#userName').val().trim();
    $('#userName').val(fullName);
    var nameLngth = $('#userName').val().length;
    var spaceIndx = $('#userName').val().indexOf(" ");

    if ($('#userName').parsley().validate() === true &&
	    (nameLngth > 4 && spaceIndx > 1 && spaceIndx < (nameLngth - 1))) // Make sure there are at least 2 names
    {
        $('#usrNameGrp .errorMsg').removeClass('filled');

        // If User Name is ok, then validate 'Email'
        var emailTrimmed = $('#userEmail').val().trim();
        $('#userEmail').val(emailTrimmed);

        if ($('#userEmail').parsley().validate() === true) {
            // If Email is ok, then validate 'Phone'
            var $strippedNum = $('#userPhone').val().trim();
            $strippedNum = $strippedNum.replace(/\D/g, "");

            $('#userPhoneHddn').val($strippedNum);

            if ($('#userPhoneHddn').parsley().validate() === true) {
                $('form#PayorInitialInfoForm').velocity("transition.slideLeftBigOut", function () {
                    $('form#crtPwFrm').velocity("transition.slideRightBigIn", function () {
                        $('#userPassword').focus();
                    });
                });
            }
            else {
                id = "#usrPhoneGrp";
            }
        }
        else {
            id = "#usrEmailGrp";
        }
    }
    else {
        if ($('#userName').parsley().validate() === true) {
            $('#userName').removeClass('parsley-success').addClass('parsley-error');
            $('#usrNameGrp .errorMsg').text('Please enter a first AND last name').addClass('parsley-errors-list').addClass('filled');
        }

        id = "#usrNameGrp";
    }

    if (id.length > 0) {
        shakeInputField(id);
    }
}

function checkPwForm() {
    //console.log("* userPassword value is: " + $('#userPassword').val() + " *");
    if ($('#userPassword').val().length == 0 ||
		($('#userPassword').val().length > 5 &&
		 $('#userPassword').parsley().validate() != false)) {
        $('#userPassword').removeClass('parsley-error');
        $('#usrPwGrp .errorMsg').removeClass('filled');

        // ADD THE LOADING BOX
        $('#body-depositNew').block({
            message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Attempting login...</span>',
            css: {
                border: 'none',
                padding: '14px 6px 10px',
                backgroundColor: '#000',
                '-webkit-border-radius': '12px',
                '-moz-border-radius': '12px',
                'border-radius': '12px',
                opacity: '.65',
                width: '160px',
                margin: '0 auto',
                top: '25px',
                color: '#fff'
            }
        });

        createRecord();
    }
    else {
        $('#userPassword').removeClass('parsley-success').addClass('parsley-error');
        $('#usrPwGrp .errorMsg').text('Please enter a slightly longer password :-)').addClass('parsley-errors-list').addClass('filled');
        shakeInputField("#usrPwGrp");
    }
}

// When Input validation fails, this function custom formats the field
// CLIFF (9/25/15): This isn't used anymore after updating ID Ver Wizard to the new style
function shakeInputField(inputGrpId) {
    $(inputGrpId + ' input').velocity("callout.shake");
    $(inputGrpId + ' .fa').css('color', '#cf1a17')
                                .velocity('callout.shake')
                                .velocity({ 'color': '#3fabe1' }, { delay: 800 });
    $(inputGrpId + ' input').focus();
}

// CLIFF (9/25/15): This also isn't used anymore after updating ID Ver Wizard to the new style
function backAstep() {
    $('form#crtPwFrm').velocity("transition.slideRightBigOut", function () {
        $('form#PayorInitialInfoForm').velocity("transition.slideLeftBigIn");
    });
}*/