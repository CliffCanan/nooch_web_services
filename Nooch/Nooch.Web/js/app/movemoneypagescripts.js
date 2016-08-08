var transType = $('#transType').val();
var transStatus = $("#pymnt_status").val();
var TRANSID = $('#transId').val();
var USERTYPE = $('#usrTyp').val();
var MemID_EXISTING = $('#memidexst').val();
var FOR_RENTSCENE = $('#rs').val();
var CIP = $('#cip').val();
var FBID = "not connected";

var usrPhn, usrEm;
var resultReason = "";
var fingprint = "";
var memIdGen = "";

var completedFirstWizard = false;
var sendToXtraVer = false;

var isSmScrn = false;
var isLrgScrn = false;

// For uploading ID Doc img
var isFileAdded = "0";
var FileData = null;

var isIdVerified = false;

var COMPANY = "Nooch";
var supportEmail = "support@nooch.com";
if (FOR_RENTSCENE == "true")
{
    COMPANY = "Rent Scene"
    supportEmail = "payments@rentscene.com"
}

$(document).ready(function () {
    // For large scrns, animate payment info to left side to be visible under the ID Ver Modal
    if ($(window).width() < 768)
    {
        isSmScrn = true;

        // CC (5/24/16): Attempt to change the input to show a number pad for smartphones - setting
        // these originally in the HTML would break the input-mask for 4-char max, and hasn't worked successfully on an iPhone test anyway so far.
        $("#idVer-ssn").attr("type", "number");
        $("#idVer-ssn").attr("pattern", "/\d*");
    }
    else if ($(window).width() > 1000)
    {
        isLrgScrn = true;
    }

    var verb = (transType == "send") ? "get paid" : "pay anyone";
    var alertBodyOpening = "Nooch is a quick, secure way to " + verb + " without having to enter a credit card.";

    if (FOR_RENTSCENE == "true")
    {
        alertBodyOpening = "Rent Scene offers a quick, secure way to " + verb + " without having to enter a credit card.";

        if (isLrgScrn)
            $('.landingHeaderLogo img').css('width', '170px');

		changeFavicon('../Assets/favicon2.ico')
    }


    if (areThereErrors() == false)
    {
        if (checkIfStillPending() == true)
        {
            if (isLrgScrn == true && USERTYPE != "NonRegistered" && USERTYPE != "Existing")
            {
                var targetWdth = '30%';
                var ms = 1250;
                if ($(window).width() > 1100)
                {
                    targetWdth = '33%';
                    if ($(window).width() > 1200)
                    {
                        targetWdth = '34%';
                        ms = 1000;
                    }
                }
                $("#payreqInfo").animate({
                    width: targetWdth
                }, ms, 'easeOutQuart');
            }
            //console.log(checkIfStillPending());

            console.log("UserType is: [" + USERTYPE + "]");

            // 1. New Users - Will Run ID Wizard
            if (USERTYPE != "NonRegistered" &&
                USERTYPE != "Existing") // Meaning this block is for the only other option is "New"  // 5/9/16: Could also be "Accepted" if it's a tenant...need to handle that too.
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

                        }, 200);
                    }
                    else {
                        window.open("https://www.nooch.com/safe");
                    }
                    //console.log(ipusr);
                });
            }
            // 2. Existing Users w/o A Bank - Open AddBank iFrame
            else if ((USERTYPE == "NonRegistered" || USERTYPE == "Existing") && 
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
                    redUrlForAddBank = (transType == "send") ? "https://www.noochme.com/noochweb/Nooch/DepositMoneyComplete?mem_id="
                                                             : "https://www.noochme.com/noochweb/Nooch/PayRequestComplete?mem_id=";
                    //redUrlForAddBank = (transType == "send") ? "http://nooch.info/Nooch/DepositMoneyComplete?mem_id="
                    //                                        : "http://nooch.info/Nooch/PayRequestComplete?mem_id=";


                    redUrlForAddBank = redUrlForAddBank + MemID_EXISTING + "," + TRANSID;

                    redUrlForAddBank = (FOR_RENTSCENE == "true") ? redUrlForAddBank + ",true"
                                                                 : redUrlForAddBank + ",false";

                    console.log("redUrlForAddBank IS: [" + redUrlForAddBank + "]");

                    $("#frame").attr("src", $('#addBank_Url').val() + "?memberid=" + MemID_EXISTING +
                                          "&redUrl=" + redUrlForAddBank);

                    $('#AddBankDiv').removeClass('hidden').addClass('bounceIn');

                    setTimeout(function () {
                        scrollToAddBank();
                    }, 1000);
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
        console.log("211. There was an error! :-(");
    }

    // Format the Memo if present
    if ($("#transMemo").text().length > 0) {
        $("#transMemo").prepend("<i class='fa fa-fw fa-commenting fa-flip-horizontal'>&nbsp;</i><em>&quot;</em>").append("<em>&quot;</em>");
    }

    $('#idVer').on('hidden.bs.modal', function (e)
    {
        if (isLrgScrn == true && isIdVerified == false) {
            $("#payreqInfo").animate({
                width: '100%',
                left: '0%'
            }, 700, 'easeInOutCubic', function ()
            {
                $('#relaunchIdwiz').removeClass('hidden').removeClass('bounceOut').addClass('bounceIn');
                setTimeout(function ()
                {
                    $('#idVerWiz').steps('destroy');
                }, 250);
            });
        }
    });
});


function runIdWizard() {
    $('#idVer').modal({
        backdrop: 'static',
        keyboard: false
    });

    $('#idVer').on('shown.bs.modal', function (e) {
        if (isLrgScrn == true) {
            $("#idVer .modal-dialog#modalContainer").animate({
                'margin-left': '45%'
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
        transitionEffect: "slideLeft",
        transitionEffectSpeed: 400,
        titleTemplate: "#title#",

        /* Labels */
        labels: {
            finish: "Submit"
        },

        /* Events */
        onInit: function (event, currentIndex) {

            if (CIP == "vendor") // Vendors only need to receive $, so Synapse doesn't require an ID, so Step 4 doesn't get displayed
                $(".wizard > .steps > ul > li").css("width", "33%");

            var heightToUse = isSmScrn ? "21em" : "22em";

            $('#idVerWiz > .content').animate({ height: heightToUse }, 300)

            $('#idVer-dob').datetimepicker({
                format: 'MM/DD/YYYY',
                useCurrent: false,
                defaultDate: moment("1980 01 01", "YYYY MM DD"),
                icons: {
                    previous: 'fa fa-fw fa-chevron-circle-left',
                    next: 'fa fa-fw fa-chevron-circle-right',
                    clear: 'fa fa-fw fa-trash-o'
                },
                maxDate: moment("1998 06 01", "MYYYY MM DD"),
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
            $('#idVer-phone').val(usrPhn);
            $('#idVer-email').val(usrEm);
            $('#idVer-phone').mask('(000) 000-0000');

            $('[data-toggle="popover"]').popover();
        },
        onStepChanging: function (event, currentIndex, newIndex) {

            if (newIndex == 0)
                $('#idVerWiz > .content').animate({ height: "22em" }, 600)

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
                            console.log($('#idVer-phone').cleanVal());

                            if ($('#idVer-phone').cleanVal().length == 10)
                            {
                                updateValidationUi("phone", true);

                                // Great, we can finally go to the next step of the wizard :-D
                                $('#idVerWiz > .content').animate({ height: "19em" }, 600)
                                return true;
                            }
                            else
                                updateValidationUi("phone", false);
                        }
                        else
                            updateValidationUi("email", false);
                    }
                    else
                        updateValidationUi("name", false);
                }
                else
                    updateValidationUi("name", false);

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
                        $('#idVerWiz > .content').animate({ height: "25em" }, 500)
                        return true;
                    }
                    else
                        updateValidationUi("zip", false);
                }
                else
                    updateValidationUi("address", false);
            }

            // IF going to Step 4
            if (newIndex == 3)
            {
                if (transType != "send")
                    return checkStepThree();
            }

            // Allways allow going backwards even if the current step is not valid
            if (currentIndex > newIndex)
                return true;
        },
        onStepChanged: function (event, currentIndex, priorIndex) {
            if (currentIndex == 1)
                $('#idVer-address').focus();
        },
        onCanceled: function (event) {
            cancelIdVer();
        },
        onFinishing: function (event, currentIndex) {

            if (transType == "send" && CIP == "vendor")
                return checkStepThree();
            else // Finish the Wizard...
                return true;
        },
        onFinished: function (event, currentIndex) {

            // ADD THE LOADING BOX
            showLoadingBox(1);

            // SUBMIT DATA TO NOOCH SERVER
            createRecord();
        }
    });
}


function checkStepThree() {
    console.log("CheckStepThree Fired\n");

    // Check DOB field
    if ($('#idVer-dob').val().length == 10)
    {
        // Double check that DOB is not still "01/01/1980", which is the default and probably not the user's B-Day...
        if ($('#idVer-dob').val() != "01/01/1980")
        {
            updateValidationUi("dob", true);

            // Check SSN field
            var ssnVal = $('#idVer-ssn').val().trim();
            ssnVal = ssnVal.replace(/ /g, "").replace(/-/g, "");

            if (ssnVal.length == 9 || FBID != "not connected")
            {
                updateValidationUi("ssn", true);

                // If a transfer (i.e. on /DepositMoney page), don't need ID, so skip that
                if (transType == "send" && CIP == "vendor")
                    return true;

                // For users paying a request... setup File Picker for uploading ID image
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
                    maxFileSize: 2500,
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
                    $('#idVerWiz > .content').animate({ height: "28em" }, 600)
                });

                $('#idVer_idDoc').on('fileloaded', function (event, file, previewId, index, reader) {
                    $('#idVerWiz > .content').animate({ height: "26em" }, 500)

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
                    isFileAdded = "0";
                    FileData = null;
                });

                $('#idVer_idDoc').on('filecleared', function (event) {
                    isFileAdded = "0";
                    FileData = null;
                });

                $('#idVerWiz > .content').animate({ height: "26em" }, 800)
                return true;
            }
            else
                updateValidationUi("ssn", false);
        }
        else
            updateValidationUi("dob-default", false);
    }
    else
        updateValidationUi("dob", false);

    return false;
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

    if (success == true)
    {
        $('#' + field + 'Grp .form-group').removeClass('has-error').addClass('has-success');
        $('#' + field + 'Grp .help-block').slideUp();

        // Show the success checkmark
        if (!$('#' + field + 'Grp .iconFeedback').length)
        {
            $('#' + field + 'Grp .form-group .fg-line').append('<i class="fa fa-check text-success iconFeedback animated bounceIn"></i>');
        }
        else
        {
            $('#' + field + 'Grp .iconFeedback').removeClass('bounceOut').addClass('bounceIn');
        }
    }
    else
    {
        $('#' + field + 'Grp .form-group').removeClass('has-success').addClass('has-error');

        // Hide the success checkmark if present
        if ($('#' + field + 'Grp .iconFeedback').length) {
            $('#' + field + 'Grp .iconFeedback').addClass('bounceOut');
        }

        var helpBlockTxt = "";
        if (field == "name") {
            helpBlockTxt = "Please enter <strong><span style='text-decoration:underline;'>your</span> full legal name</strong>.";
        }
        else if (field == "dob") {
            helpBlockTxt = "Please enter your date of birth. &nbsp;Only needed to verify your ID!"
        }
        else if (field == "ssn") {
            helpBlockTxt = isSmScrn ? "Please enter your <strong>SSN</strong>."
                                    : "<strong>Please enter your SSN.</strong>"// or connect with FB." // CC (6/7/16): Un-comment once Synapse finishes adding /user/docs/add to V3.0

            if (isSmScrn)
            {
                $('#idVerWiz > .content').animate({ height: "26em" }, 300)
            }
        }
        else if (field == "address") {
            helpBlockTxt = "Please enter <strong>just the <span style='text-decoration:underline;'>street address</span></strong> of where you <strong>currently</strong> live."
        }
        else if (field == "zip") {
            helpBlockTxt = "Please enter the ZIP code for the street address above."
        }
        else if (field == "email") {
            helpBlockTxt = "Please enter a valid email address that you own."
        }
        else if (field == "phone") {
            helpBlockTxt = "Please enter a valid 10-digit phone number."
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
    var ssnVal = $('#idVer-ssn').val().trim().replace(/ /g, "").replace(/-/g, "");
    var dobVal = $('#idVer-dob').val().trim();
    var addressVal = $('#idVer-address').val().trim();
    var zipVal = $('#idVer-zip').val().trim();
    var fngprntVal = fingprint;
    var ipVal = ipusr;
	var isImageAdded = isFileAdded;
    var imageData = FileData;
    var isRentScene = FOR_RENTSCENE == "true" ? true : false;

    console.log("{transId: " + TRANSID + ", userEm: " + userEmVal +
				", userPh: " + userPhVal + ", userName: " + userNameVal +
                ", userPw: " + userPwVal + ", ssn: " + ssnVal +
				", dob: " + dobVal + ", fngprnt: " + fngprntVal +
				", ip: " + ipVal + ", isIdImage: " + isImageAdded +
				", CIP: " + CIP + ", FBID: " + FBID +
                ", isRentScene: " + isRentScene + "}");

    var urlToUse = "";
    if (transType == "send")
    {
        urlToUse = "RegisterUserWithSynpForDepositMoney";
    }
    else // must be a Request or Rent payment (which also uses the payRequest page)
    { 
        urlToUse = "RegisterUserWithSynpForPayRequest";
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
        "', 'ip':'" + ipVal +
        "', 'cip':'" + CIP +
        "', 'fbid':'" + FBID +
        "', 'isRentScene':" + isRentScene +
        ", 'isIdImage':'" + isFileAdded +
        "', 'idImagedata':'" + FileData + "'}";
    
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
			    $(".errorMessage").addClass('hidden');

			    memIdGen = RegisterUserWithSynpResult.memberIdGenerated;

			    // Check if user's SSN verification was successful
			    if (RegisterUserWithSynpResult.ssn_verify_status != null &&
                    RegisterUserWithSynpResult.ssn_verify_status.indexOf("additional") > -1)
			    {
			        // Will need to send user to answer ID verification questions after selecting bank account
			        console.log("Need to answer ID verification questions");

			        sendToXtraVer = true;
			        completedFirstWizard = true;

			        $("#idVerWiz").addClass("animated bounceOut");

			        var idVerURL = $('#idVer_Url').val();

			        $("#idVerContainer iframe").attr("src", idVerURL + "?memid=" + memIdGen + "&from=lndngpg");

			        setTimeout(function () {
			            $("#idVerWiz").css({
			                "height": "0",
			                "padding": "0"
			            });
			            $("#idVer .modal-body").css("padding-top", "0");
			            $("#idVerContainer").addClass("bounceIn").removeClass("hidden");
			        }, 1000);
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

			        if (resultReason.indexOf('Validation PIN sent') > -1) {
			            $('#idVer').modal('toggle');

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

			                if (inputTxt === "") {
			                    swal.showInputError("Please enter the PIN sent to your phone.");
			                    return false
			                }
			                if (inputTxt.length < 4) {
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


function submitPin(pin) {
    console.log("submitPin fired - PIN [" + pin + "]");

    var memId = $('#memidexst').val();

    console.log("SubmitPIN Payload -> {memId: " + memId + ", PIN: " + pin + "}");

    // ADD THE LOADING BOX
    $.blockUI({
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
             "', 'pin':'" + pin + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (result) {
            console.log("SUCCESS -> SubmitPIN result is... [next line]");
            console.log(result);

            resultReason = result.msg;

            // Hide the Loading Block
            $.unblockUI();

            if (result.success == true) {
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
                }, function (isConfirm) {
                    // SUBMIT ID WIZARD DATA TO SERVER AGAIN...
                    console.log("Calling createRecord() again...")
                    createRecord();
                });
            }
            else {
                console.log("Success != true");

                if (resultReason != null) {
                    console.log(resultReason);

                    if (resultReason.indexOf('Validation PIN sent') > -1) {
                        swal({
                            title: "Check Your Phone",
                            text: "To verify your phone number, we just sent a text message to your phone.  Please enter the <strong>PIN</strong> to continue.</span>" +
								  "<i class='show fa fa-mobile' style='font-size:40px; margin: 10px 0 0;'></i>",
                            type: "input",
                            showCancelButton: true,
                            confirmButtonColor: "#3fabe1",
                            confirmButtonText: "Ok",
                            html: true
                        }, function (inputTxt) {
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
        Error: function (x, e) {
            // Hide the Loading Block
            $.unblockUI();

            console.log("Submit PIN ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('3');
        }
    });
}


function idVerifiedSuccess() {
    isIdVerified = true;

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
            text: "Thanks for submitting your ID information. That helps us keep " + COMPANY + " safe for everyone." +
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
            
            redUrlForAddBank = (transType == "send") ? "https://www.noochme.com/noochweb/Nooch/DepositMoneyComplete?mem_id="
                                                     : "https://www.noochme.com/noochweb/Nooch/PayRequestComplete?mem_id=";
            //redUrlForAddBank = (transType == "send") ? "http://nooch.info/noochweb/Nooch/DepositMoneyComplete?mem_id="
            //                                         : "http://nooch.info/noochweb/Nooch/PayRequestComplete?mem_id=";

            redUrlForAddBank = redUrlForAddBank + memIdGen + "," + TRANSID;

            redUrlForAddBank = (FOR_RENTSCENE == "true") ? redUrlForAddBank + ",true"
                                                         : redUrlForAddBank + ",false";

            console.log("redUrlForAddBank IS: [" + redUrlForAddBank + "]"); 

            $("#frame").attr("src", $('#addBank_Url').val()+ "?memberid=" + memIdGen +
                                          "&redUrl=" + redUrlForAddBank);

            $('#AddBankDiv').removeClass('hidden').addClass('bounceIn');

            setTimeout(function ()
            {
                scrollToAddBank();
            }, 1000);
        });
    }, 400);
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
    else if (n == 3) {
        msg = "Submitting...";
    }
    else {
        msg = "Submitting responses...";
    }

    $('.modal-content').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">' + msg + '</span>',
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
	    alertBodyText = "We had trouble finding that transaction.  Please try again and if you continue to see this message, contact <span style='font-weight:600;'>" + COMPANY + " Support</span>:" +
                        "<br/><a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
	}
	else if (errorNum == '2') // Errors after submitting ID verification AJAX
	{
	    alertTitle = "Errors Are The Worst!";
	    alertBodyText = "Terrible sorry, but it looks like we had trouble processing your info.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
                        COMPANY + " Support</span>:<br/><a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
	}
	else if (errorNum == '3') // Error rejecting a payment
	{
	    alertTitle = "Errors Are Annoying";
	    alertBodyText = "Our apologies, but we were not able to reject that request.  Please try again or contact <span style='font-weight:600;'>" + COMPANY + " Support</span>:" +
                        "<br/><a href='" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
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
            
        
        alertBodyText += "&nbsp;&nbsp;Please contact <span style='font-weight:600;'>" + COMPANY + " Support</span> if you believe this is an error.<br/>" +
                         "<a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";

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
			$.blockUI({
				message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Attempting Payment...</span>',
				css: {
					border: 'none',
					padding: '26px 8px 20px',
					backgroundColor: '#000',
					'-webkit-border-radius': '14px',
					'-moz-border-radius': '14px',
					'border-radius': '14px',
					opacity: '.8',
					margin: '0 auto',
					color: '#fff'
				}
			});

            var redUrlToSendTo = "";

            redUrlToSendTo = (transType == "send") ? "https://www.noochme.com/noochweb/Nooch/DepositMoneyComplete?mem_id="
                                                   : "https://www.noochme.com/noochweb/Nooch/PayRequestComplete?mem_id=";
            //redUrlToSendTo = (transType == "send") ? "http://nooch.info/noochweb/Nooch/DepositMoneyComplete?mem_id="
            //                                       : "http://nooch.info/noochweb/Nooch/PayRequestComplete?mem_id=";

            redUrlToSendTo = redUrlToSendTo + MemID_EXISTING + "," + TRANSID;

            redUrlToSendTo = (FOR_RENTSCENE == "true") ? redUrlToSendTo + ",true"
                                                       : redUrlToSendTo + ",false";

            console.log("redUrlToSendTo IS: [" + redUrlToSendTo + "]");

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

    //window.location = "https://www.noochme.com/noochweb/Nooch/rejectMoney?TransactionId=" + TRANSID +
    //                  "&UserType=" + userTypeEncr +
    //                  "&TransType=T3EMY1WWZ9IscHIj3dbcNw==";

    //ADD THE LOADING BOX
    $.blockUI({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Rejecting This Request...</span>',
        css: {
            border: 'none',
            padding: '26px 8px 20px',
            backgroundColor: '#000',
            '-webkit-border-radius': '15px',
            '-moz-border-radius': '15px',
            'border-radius': '15px',
            opacity: '.8',
            margin: '0 auto',
            color: '#fff'
        }
    });

    $.ajax({
        type: "POST",
        url: $('#rejectMoneyLink').val() + "?TransactionId=" + TRANSID + "&UserType=" + userTypeEncr + "&TransType=T3EMY1WWZ9IscHIj3dbcNw==",
        success: function (msg)
        {
            console.log("SUCCESS -> RejectMoneyResult is... ");
            console.log(msg);

            // Hide the Loading Block
            $.unblockUI()

            if (typeof msg.errorFromCodeBehind != 'undefined' &&
			    (msg.errorFromCodeBehind.indexOf("no longer pending") > -1 || msg.transStatus.indexOf("no longer pending") > -1))
            {
                console.log("This payment was no longer pending.");
                transStatus = "not pending";
                checkIfStillPending();
            }
            else if (msg.errorFromCodeBehind = "0")
            {
                $("#transResult").text("Request Rejected Successfully");
                $("#nonRegUsrContainer").fadeOut('fast');

                var firstName = $('#senderName1').text().trim().split(" ");

                swal({
                    title: "Payment Rejected",
                    text: "You just rejected that payment request successfully.<br/><br/>Hope " + firstName[0] + " won't mind!",
                    type: "success",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "OK",
                    closeOnConfirm: true,
                    customClass: "largeText",
                    html: true
                });
            }
            else {
                console.log("Response was not successful :-(");
                showErrorAlert('3');
            }
        },
        Error: function (x, e)
        {
            //   Hide the Loading Block
            $.unblockUI()

            console.log("ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('3');
        }
    });
}


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


// -----------------
// UNUSED FUNCTIONS
// -----------------
/*
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
}*/
