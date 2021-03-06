﻿var errorId = $('#errorId').val();
var FROM = $('#from').val();
var COMPANY = "Nooch";

var isRequest;
var amount, name, email, memo, pin, ipVal, userType;

var existNAME, existMEMID;

var askForPin = false;
var pinVerified = false;

$(document).ready(function () {
    var isSmScrn = ($(window).width() < 768) ? true : false;

    if (FROM == "habitat")
    {
        COMPANY = "Habitat";

        if (isSmScrn)
            $('.navbar img').css('width', '100px');

        $('#navbar-title').css('top', '12px');

        changeFavicon('../Assets/favicon-habitat.png')

        askForPin = true;
        var suggestedUsers = getSuggestedUsers();
    }
    else if (FROM == "appjaxx" || FROM == "josh")
    {
        askForPin = true;
        var suggestedUsers = getSuggestedUsers();
    }

    if (askForPin)
        showPinPrompt("initial", FROM);
    else
    {
        setTimeout(function () {
            $('#amount').focus();
        }, 200)
    }

    //console.log(ipusr);

    $('#amount').mask("#,##0.00", { reverse: true });

    $('[data-toggle="popover"]').popover();

    $('#submitPayment').click(function () {
        checkFormData();
        return false;
    });

    $('input[name="userType"]').change(function () {
        if ($('input[name="userType"]:checked').val() == 'vendor')
        {
            if ($('input[name="type"]:checked').val() == 'request')
                return false;
        }
    });


    $("input[name='type']").change(function () {
        if ($('input[name="type"]:checked').val() == 'request')
        {
            $('#typeGrp .btn').removeClass('btn-success').addClass('btn-primary');
            $('#submitPayment').removeClass('btn-success').addClass('btn-primary');
            $('#vendor').closest('label').addClass('disabled');
        }
        else // Send has been clicked
        {
            $('#typeGrp .btn').removeClass('btn-primary').addClass('btn-success');
            $('#submitPayment').removeClass('btn-primary').addClass('btn-success');
            $('#vendor').closest('label').removeClass('disabled');
        }
    });
});


function formatAmount() {
    var formattedAmount = $('#amount').val().trim().replace(",", "");

    if (formattedAmount.length > 0)
    {
        if (formattedAmount.length <= 2)
            $('#amount').val(formattedAmount + '.00');

        if (Number(formattedAmount) < 5 || Number(formattedAmount) > 500)
            updateValidationUi("amount", false);
        else
            updateValidationUi("amount", true);
    }
    else
        updateValidationUi("amount", false);
}


function checkFormData() {
    console.log('submitPayment Initiated...');

    // CHECK TO MAKE SURE ALL FIELDS WERE COMPLETED
    var amountVal = Number($('#amount').val().replace(",", ""));

    if ($('#amount').val().length > 0 &&
        amountVal >= 5 &&
        amountVal <= 5000)
    {
        updateValidationUi("amount", true);

        if ($('#name').val().length > 3)
        {
            var trimmedName = $('#name').val().trim();
            $('#name').val(trimmedName);

            // Check Name Field for a " "
            if ($('#name').val().indexOf(' ') > 1)
            {
                updateValidationUi("name", true);

                var emailVal = $('#email').val().trim();
                // Check Email field
                if (emailVal.length > 2 &&
                    ValidateEmail(emailVal))
                {
                    updateValidationUi("email", true);

                    // Check Memo field
                    if ($('#memo').val().length > 2)
                    {
                        updateValidationUi("memo", true);

                        // Great, we can finally submit the payment info
                        submitPayment();
                    }
                    else updateValidationUi("memo", false);
                }
                else updateValidationUi("email", false);
            }
            else updateValidationUi("name", false);
        }
        else updateValidationUi("name", false);
    }
    else
        updateValidationUi("amount", false);

    return;
}


function submitPayment() {
    if (askForPin && !pinVerified)
        return;

    var transType = $('input[name="type"]:checked').val();
    isRequest = transType == "send" ? false : true;

    // ADD THE LOADING BOX
    var loadingTxt = isRequest ? "Payment Request" : "Payment"
    $('#makePaymentContainer').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Submitting ' + loadingTxt + '...</span>',
        css: {
            border: 'none',
            padding: '25px 8px 20px',
            backgroundColor: '#000',
            '-webkit-border-radius': '14px',
            '-moz-border-radius': '14px',
            'border-radius': '14px',
            opacity: '.8',
            width: '270px',
            margin: '0 auto',
            color: '#fff'
        }
    });

    // Hide errorMessage <div> if showing
    if (!$(".errorMessage").hasClass('hidden'))
    {
        $(".errorMessage").addClass('hidden');
        $(".errorMessage").html('');
        $(".errorMessage a").addClass("bounceOut");
    }

    amount = $('#amount').val();
    name = $('#name').val();
    email = $('#email').val().trim();
    memo = $('#memo').val().trim().replace("'", "%27");
    pin = "";
    ipVal = ipusr;
    userType = $('input[name="userType"]:checked').val();

    console.log("SUBMIT PAYMENT -> {from: " + FROM +
                                 ", isRequest: " + isRequest +
                                 ", amount: " + amount +
                                 ", name: " + name +
                                 ", email: " + email +
                                 ", memo: " + memo +
                                 ", pin: " + pin +
                                 ", ipVal: " + ipVal +
                                 ", userType: " + userType + "}");

    $.ajax({
        type: "POST",
        url: URLs.submitPayment,
        data: "{'from':'" + FROM +
		      "', 'isRequest':'" + isRequest +
              "', 'amount':'" + amount +
              "', 'name':'" + name +
              "', 'email':'" + email +
              "', 'memo':'" + memo +
              "', 'pin':'" + pin +
              "', 'ip':'" + ipVal +
              "', 'cip':'" + userType + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
            var sendPaymentResponse = msg;
            console.log("SUCCESS -> 'sendPaymentResponse' is... ");
            console.log(sendPaymentResponse);

            var resultReason = sendPaymentResponse.msg;

            // Hide the Loading Block
            $('#makePaymentContainer').unblock();

            if (sendPaymentResponse.name != null)
                existNAME = sendPaymentResponse.name;
            if (sendPaymentResponse.memberId != null)
                existMEMID = sendPaymentResponse.memberId;

            if (sendPaymentResponse.success == true)
            {
                // request type
                if (isRequest == true)
                {
                    if (sendPaymentResponse.isEmailAlreadyReg == true)
                    {
                        var status = "Active";
                        var statusCssClass = "f-600";

                        if (sendPaymentResponse.memberStatus == "Suspended" ||
                            sendPaymentResponse.memberStatus == "Temporarily_Blocked")
                        {
                            status = "Suspended";
                            statusCssClass = "label label-danger";
                        }
                        else if (sendPaymentResponse.memberStatus == "Active" ||
                                 sendPaymentResponse.memberStatus == "NonRegistered")
                        {
                            status = "Active";
                            statusCssClass = "label label-success"
                        }
                        else if (sendPaymentResponse.memberStatus != null)
                        {
                            status = sendPaymentResponse.memberStatus;
                            statusCssClass = "";
                        }

                        var bodyText = "<table border='0' width='95%' cellpadding='4' style='font-size: 16px; margin:12px auto 20px;'><tbody>" +
                                       "<tr><td style='vertical-align:top'>Name:</td><td><strong>" + sendPaymentResponse.name + "</strong><span class='show m-b-5' style='line-height:1;'>" + email + "</span></td></tr>" +
                                       "<tr><td>Status:</td><td><span class='" + statusCssClass + "'>" + status + "</span></td></tr>" +
                                       "<tr><td>Date Created:</td><td>" + sendPaymentResponse.dateCreated + "</td></tr>" +
                                       "<tr><td>Bank Linked:</td><td>" + sendPaymentResponse.isBankAttached + "</td></tr>";

                        if (sendPaymentResponse.isBankAttached == true)
                            bodyText = bodyText + "<tr><td>Bank Status:</td><td>" + sendPaymentResponse.bankStatus + "</td></tr>";

                        bodyText = bodyText + "</tbody></table>";

                        // THEN DISPLAY SUCCESS ALERT...
                        swal({
                            title: "Email Already Registered",
                            text: bodyText +
                                  "<span class='show f-600' style='margin: 10px 30px;'>Do you still want to send a payment request to <strong>" + sendPaymentResponse.name + "</strong>?</span>",
                            type: "warning",
                            showCancelButton: true,
                            confirmButtonColor: "#3fabe1",
                            confirmButtonText: "Send",
                            html: true
                        }, function (isConfirm) {
                            if (isConfirm)
                                sendRequestToExistingUser();
                        });
                    }
                    else
                    {
                        $('.alert.alert-success #resultAmount').text('$' + amount);
                        $('.alert.alert-success #resultName').text(name);
                        $('.alert.alert-success').removeClass('hidden').slideDown();

                        // THEN DISPLAY SUCCESS ALERT...
                        swal({
                            title: "Payment Created Successfully",
                            text: "<table border='0' width='95%' cellpadding='8'><tbody>" +
                                  "<tr><td>Amount</td><td>$" + amount + "</td></tr>" +
                                  "<tr><td>Name</td><td>" + name + "</td></tr>" +
                                  "<tr><td>Email</td><td>" + email + "</td></tr>" +
                                  "<tr><td>Memo</td><td>" + memo + "</td></tr></tbody></table>" +
                                  "<span class='show m-t-10'><strong>" + name + "</strong> has been notified via email.</span>",
                            type: "success",
                            showCancelButton: false,
                            confirmButtonColor: "#3fabe1",
                            confirmButtonText: "Ok",
                            html: true
                        }, function () {
                            resetForm();
                        });
                    }
                }
                else // send type
                {
                    if (sendPaymentResponse.isEmailAlreadyReg == true)
                    {
                        var status = "Active";
                        var statusCssClass = "f-600";

                        if (sendPaymentResponse.memberStatus == "Suspended" ||
                            sendPaymentResponse.memberStatus == "Temporarily_Blocked")
                        {
                            status = "Suspended";
                            statusCssClass = "label label-danger";
                        }
                        else if (sendPaymentResponse.memberStatus == "Active" ||
                                 sendPaymentResponse.memberStatus == "NonRegistered")
                        {
                            status = "Active";
                            statusCssClass = "label label-success"
                        }
                        else if (sendPaymentResponse.memberStatus != null)
                        {
                            status = sendPaymentResponse.memberStatus;
                            statusCssClass = "";
                        }

                        var bodyText = "<table border='0' width='95%' cellpadding='4' style='font-size: 16px; margin:12px auto 20px;'><tbody>" +
                                       "<tr><td style='vertical-align:top'>Name:</td><td><strong>" + sendPaymentResponse.name + "</strong><span class='show m-b-5' style='line-height:1;'>" + email + "</span></td></tr>" +
                                       "<tr><td>Status:</td><td><span class='" + statusCssClass + "'>" + status + "</span></td></tr>" +
                                       "<tr><td>Date Created:</td><td>" + sendPaymentResponse.dateCreated + "</td></tr>" +
                                       "<tr><td>Bank Linked:</td><td>" + sendPaymentResponse.isBankAttached + "</td></tr>";

                        if (sendPaymentResponse.isBankAttached == true)
                            bodyText = bodyText + "<tr><td>Bank Status:</td><td>" + sendPaymentResponse.bankStatus + "</td></tr>";

                        bodyText = bodyText + "</tbody></table>";

                        // THEN DISPLAY SUCCESS ALERT...
                        swal({
                            title: "Email Already Registered",
                            text: bodyText +
                                  "<span class='show f-600' style='margin: 10px 30px;'>Do you still want to transfer money to " + sendPaymentResponse.name + "?</span>",
                            type: "warning",
                            showCancelButton: true,
                            confirmButtonColor: "#3fabe1",
                            confirmButtonText: "Send",
                            html: true
                        }, function (isConfirm) {
                            if (isConfirm)
                                sendRequestToExistingUser();
                        });
                    }
                    else
                    {
                        $('.alert.alert-success #resultAmount').text('$' + amount);
                        $('.alert.alert-success #resultName').text(name);
                        $('.alert.alert-success').removeClass('hidden').slideDown();

                        // THEN DISPLAY SUCCESS ALERT...
                        swal({
                            title: "Money Transferred Successfully",
                            text: "<table border='0' width='95%' cellpadding='8'><tbody>" +
                                  "<tr><td>Amount</td><td>$" + amount + "</td></tr>" +
                                  "<tr><td>Name</td><td>" + name + "</td></tr>" +
                                  "<tr><td>Email</td><td>" + email + "</td></tr>" +
                                  "<tr><td>Memo</td><td>" + memo + "</td></tr></tbody></table>",
                            type: "success",
                            showCancelButton: false,
                            confirmButtonColor: "#3fabe1",
                            confirmButtonText: "Ok",
                            html: true
                        }, function () {
                            resetForm();
                        });
                    }

                }
            }
            else
            {
                if (resultReason != null)
                {
                    if (sendPaymentResponse.name != null)
                        name = sendPaymentResponse.name;

                    if (resultReason.indexOf("No Synapse user or bank") > -1)
                        showErrorAlert('30');
                    else if (resultReason.indexOf("No Synapse user info found") > -1)
                        showErrorAlert('31');
                    else if (resultReason.indexOf("but no bank account linked") > -1)
                        showErrorAlert('32');
                    else if (resultReason.indexOf("but account is unverified") > -1)
                        showErrorAlert('40');
                    else if (resultReason.indexOf("bank not allowed") > -1)
                        showErrorAlert('41');

                    else if (resultReason.indexOf("email already registered") > -1)
                        showErrorAlert('20');
                    else if (resultReason.indexOf("Requester does not have any verified bank account") > -1)
                        showErrorAlert('4');
                    else if (resultReason.indexOf("Missing") > -1)
                        showErrorAlert('5');
                    else if (resultReason.indexOf("Sender does not have any bank") > -1 ||
                             resultReason.indexOf("Requester does not have any bank added") > -1)
                        showErrorAlert('6');
                    else if (resultReason.indexOf("Sender has insufficient permissions") > -1)
                        showErrorAlert('7')
                    else
                        showErrorAlert('2');
                }
                else
                    showErrorAlert('2');
            }
        },
        Error: function (x, e) {
            // Hide the Loading Block
            $('#makePaymentContainer').unblock();

            console.log("ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('2');
        }
    });
}

// This can only be called for SENDING payments IF the recipient has a fully verified user & bank account w/ Nooch & Synapse.
function sendRequestToExistingUser() {
    var transType = $('input[name="type"]:checked').val();
    isRequest = transType == "send" ? false : true;

    // ADD THE LOADING BOX
    var loadingTxt = isRequest ? "Payment Request" : "Payment"
    $('#makePaymentContainer').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Submitting ' + loadingTxt + '...</span>',
        css: {
            border: 'none',
            padding: '25px 8px 20px',
            backgroundColor: '#000',
            '-webkit-border-radius': '14px',
            '-moz-border-radius': '14px',
            'border-radius': '14px',
            opacity: '.8',
            width: '270px',
            margin: '0 auto',
            color: '#fff'
        }
    });

    console.log("SUBMIT PAYMENT (2nd time - to existing user) -> {From: " + FROM +
                                 ", isRequest: " + isRequest +
                                 ", amount: " + amount +
                                 ", nameEntered: " + name +
                                 ", nameFromServer: " + existNAME +
                                 ", email: " + email +
                                 ", memo: " + memo +
                                 ", MemID: " + existMEMID +
                                 ", pin: " + pin +
                                 ", ipVal: " + ipVal + "}");

    $.ajax({
        type: "POST",
        url: "submitRequestToExistingUser",
        data: "{'from':'" + FROM +
              "', 'isRequest':'" + isRequest +
              "', 'amount':'" + amount +
              "', 'name':'" + name +
              "', 'email':'" + email +
              "', 'memo':'" + memo +
              "', 'pin':'" + pin +
              "', 'ip':'" + ipVal +
              "', 'memberId':'" + existMEMID +
              "', 'nameFromServer':'" + existNAME + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
            var sendPaymentResponse = msg;
            console.log("SUCCESS -> 'sendPaymentResponse' is... ");
            console.log(sendPaymentResponse);

            var resultReason = sendPaymentResponse;

            // Hide the Loading Block
            $('#makePaymentContainer').unblock();

            if (sendPaymentResponse.success == true)
            {
                $('.alert.alert-success #resultAmount').text('$' + amount);
                $('.alert.alert-success #resultName').text(existNAME);
                $('.alert.alert-success').removeClass('hidden').slideDown();

                // THEN DISPLAY SUCCESS ALERT...
                if (isRequest == true)
                {
                    swal({
                        title: "Payment Created Successfully",
                        text: "<table border='0' width='95%' cellpadding='8'><tbody>" +
                              "<tr><td>Amount</td><td>$" + amount + "</td></tr>" +
                              "<tr><td>Name</td><td>" + existNAME + "</td></tr>" +
                              "<tr><td>Email</td><td>" + email + "</td></tr>" +
                              "<tr><td>Memo</td><td>" + memo + "</td></tr></tbody></table>" +
                              "<span class='show m-t-10'><strong>" + existNAME + "</strong> has been notified via email.</span>",
                        type: "success",
                        showCancelButton: false,
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Ok",
                        html: true
                    }, function () {
                        resetForm();
                    });
                }
                else
                {
                    swal({
                        title: "Payment Scheduled Successfully",
                        text: "<table border='0' width='95%' cellpadding='8'><tbody>" +
                              "<tr><td>Amount</td><td>$" + amount + "</td></tr>" +
                              "<tr><td>Name</td><td>" + existNAME + "</td></tr>" +
                              "<tr><td>Email</td><td>" + email + "</td></tr>" +
                              "<tr><td>Memo</td><td>" + memo + "</td></tr></tbody></table>" +
                              "<span class='show m-t-10'><strong>" + existNAME + "</strong> has been notified via email.</span>",
                        type: "success",
                        showCancelButton: false,
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Ok",
                        html: true
                    }, function () {
                        resetForm();
                    });
                }
            }
            else showErrorAlert('2');
        },
        Error: function (x, e) {
            // Hide the Loading Block
            $('#makePaymentContainer').unblock();

            console.log("ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('2');
        }
    });
}


function updateValidationUi(field, success) {
    //console.log("Field: " + field + "; success: " + success);

    if (success == true)
    {
        $('#' + field + 'Grp').removeClass('has-error').addClass('has-success');
        $('#' + field + 'Grp .help-block').slideUp();

        // Show the success checkmark
        if (!$('#' + field + 'Grp .iconFeedback').length)
            $('#' + field + 'Grp .fg-line').append('<i class="fa fa-check text-success iconFeedback animated bounceIn"></i>');
        else
            $('#' + field + 'Grp .iconFeedback').removeClass('bounceOut').addClass('bounceIn');

        if (field == "name")
            $('#name').addClass('capitalize');
    }
    else
    {
        $('#' + field + 'Grp').removeClass('has-success').addClass('has-error');

        // Hide the success checkmark if present
        if ($('#' + field + 'Grp .iconFeedback').length)
            $('#' + field + 'Grp .iconFeedback').addClass('bounceOut');

        var helpBlockTxt = "";
        if (field == "amount")
            helpBlockTxt = "Please enter an amount between <strong>$5 - $100</strong>.";
        else if (field == "name")
            helpBlockTxt = "Please enter a name for this recipient."
        else if (field == "email")
            helpBlockTxt = "Please enter a valid email."
        else if (field == "memo")
            helpBlockTxt = "Please enter a descriptive memo so that the recipient knows what this payment is for."

        if (!$('#' + field + 'Grp .inputContainer .help-block').length)
        {
            $('#' + field + 'Grp .inputContainer').append('<small class="help-block" style="display:none">' + helpBlockTxt + '</small>');
            $('#' + field + 'Grp .inputContainer .help-block').slideDown();
        }
        else
            $('#' + field + 'Grp .inputContainer .help-block').show()

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

    if (lstr < 5) return false;

    if (lat == -1 || lat == 0 || lat == lstr) return false

    if (ldot == -1 || ldot == 0 || ldot > lstr - 3) return false

    if (str.indexOf(at, (lat + 1)) != -1) return false

    if (str.substring(lat - 1, lat) == dot || str.substring(lat + 1, lat + 2) == dot) return false

    if (str.indexOf(dot, (lat + 2)) == -1) return false

    if (str.indexOf(" ") != -1) return false

    return true
};


function resetForm() {
    console.log("Resetting form...");

    $('#paymentForm .form-group').removeClass('has-error has-success');
    $('.iconFeedback').addClass('bounceOut');
    $('.help-block').slideUp();

    $('#amount').val('');
    $('#name').val('');
    $('#email').val('');
    $('#memo').val('');
}

function resetName() {
    $('#name').val('').focus();
}


function showErrorAlert(errorNum) {
    var alertTitle = "";
    var alertBodyText = "";
    var shouldFocusOnEmail = false;
    var shouldShowErrorDiv = true;

    console.log("ShowError -> errorNum is: [" + errorNum + "]");

    if (errorNum == '1') // Codebehind errors
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "We had trouble finding that transaction.  Please try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<a href='mailto:support@nooch.com?subject=Support%20Request%20-%20Make%20Payment%20Page%20Error-1' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '2' || errorNum == '3') // Errors after submitting ID verification AJAX
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing your info. &nbsp;Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<a href='mailto:support@nooch.com?subject=Support%20Request%20-%20Make%20Payment%20Page%20Error-" + errorNum + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '4') // Requester does not have any verified bank account
    {
        alertTitle = "Oh No - Error!";
        alertBodyText = "Looks like the requester's account (yours) does not have a verified bank attached.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<a href='mailto:support@nooch.com?subject=Support%20Request%20-%20Make%20Payment%20Page%20Error-4' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '5') // Missing some piece of data (shouldn't ever happen b/c the form shouldn't submit if missing anything)
    {
        alertTitle = "Oh No - Error!";
        alertBodyText = "Looks like we were missing some required information. &nbsp;Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<a href='mailto:support@nooch.com?subject=Support%20Request%20-%20Make%20Payment%20Page%20Error-5' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '6') // Sender has no active bank account linked
    {
        alertTitle = "Missing Funding Source";
        alertBodyText = "Looks like your account don't have a funding source fully set up yet. Please contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<a href='mailto:support@nooch.com?subject=Support%20Request%20-%20Make%20Payment%20Page%20Error-6' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '7') // Sender has no active bank account linked
    {
        alertTitle = "Incomplete Account";
        alertBodyText = "Looks like your account isn't fully verified to send money yet. Please contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<a href='mailto:support@nooch.com?subject=Support%20Request%20-%20Make%20Payment%20Page%20Error-7' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '20') // EMAIL came back as already registered with Nooch.
    {
        alertTitle = "Email Already Registered";
        alertBodyText = "Looks like <strong>" + $('#idVer-email').val() + "</strong> is already registered to a Nooch account. &nbsp;Please try a different email address.";
        shouldFocusOnEmail = true;
        shouldShowErrorDiv = false;
    }
    else if (errorNum == '30' || errorNum == '31' || errorNum == '32') // EMAIL came back as already registered with Nooch.
    {
        var nameLocal = existNAME != null ? existNAME : name;
        var fName = "the user";
        var lName = "";
        if (nameLocal.indexOf(' ') > 0)
            fName = nameLocal.slice(0, nameLocal.indexOf(' '))

        alertTitle = "Recipient's Bank Missing";

        if (existMEMID != null && existMEMID.length > 30)
            alertBodyText = "Looks like <strong>" + nameLocal + "</strong> already has a Nooch account, but it is not fully active yet." +
                            "<span class='block m-t-10'>Please direct " + fName + " to <a href='https://www.noochme.com/noochweb/Nooch/createAccount?memId=" + existMEMID + "&by=habitat' target='_blank'>this personalized link</a> to finish linking a bank account:</span>" +
                            "<a href='https://www.noochme.com/noochweb/Nooch/createAccount?memId=" + existMEMID + "&by=habitat' target='_blank' style='font-size: 84%; line-height: 1.3; margin-top: 12px;' class='block'>https://www.noochme.com/noochweb/Nooch/createAccount?memId=" + existMEMID + "&by=habitat<a>";
        else
            alertBodyText = "Looks like <strong>" + name + "</strong> already has a Nooch account, but it is not fully active yet." +
                            "<span class='block m-t-10'>Please contact <a href='mailto:support@nooch.com?subject=Support%20Request%20-%20Make%20Payment%20Page%20Error-" + errorNum + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>Nooch Support</a> for further assistance.";

        shouldShowErrorDiv = false;
    }
    else if (errorNum == '40') // User's Synapse permission is Unverified
    {
        var nameLocal = existNAME != null ? existNAME : name;
        var fName = "the user";
        var lName = "";
        if (nameLocal.indexOf(' ') > 0)
            fName = nameLocal.slice(0, nameLocal.indexOf(' '))

        alertTitle = fName == "the user" ? "Recipient Is Unverified" : fName + "'s ID is Unverified";

        alertBodyText = "Good news is <strong>" + fName + "</strong> already has a Nooch account. The bad news is " + fName + "'s ID is not fully verified yet." +
                        "<span class='block m-t-10'>Please contact <a href='mailto:support@nooch.com?subject=Support%20Request%20-%20Attempted%20to%20pay%20Unverified%20User' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>Nooch Support</a> for further assistance.";

        shouldShowErrorDiv = false;
    }
    else if (errorNum == '41') // User's Bank is Unverified or Locked
    {
        var nameLocal = existNAME != null ? existNAME : name;
        var fName = "the user";
        var lName = "";
        if (nameLocal.indexOf(' ') > 0)
            fName = nameLocal.slice(0, nameLocal.indexOf(' '))

        alertTitle = fName == "the user" ? "Recipient's Bank Is Unverified" : fName + "'s Bank is Unverified";

        alertBodyText = "Good news is <strong>" + nameLocal + "</strong> already has a Nooch account. The bad news is " + fName + "'s bank is not fully verified yet." +
                        "<span class='block m-t-10'>Please contact <a href='mailto:support@nooch.com?subject=Support%20Request%20-%20Attempted%20to%20pay%20Unverified%20Bank' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>Nooch Support</a> for further assistance.";

        shouldShowErrorDiv = false;
    }
    else // Generic Error
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing that request. &nbsp;Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support:</span>" +
                        "<br/><a href='mailto:support@nooch.com?subject=Support%20Request%20-%20Make%20Payment%20Page%20Error' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    console.log("Should Show Error Div is...");
    console.log(shouldShowErrorDiv);

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
        if (!isConfirm)
            window.open("mailto:support@nooch.com?subject=Support%20Request%20-%20Attempted%20Payment%20Failed");
        else if (shouldFocusOnEmail)
            updateValidationUi("email", false);
    });
}


function showPinPrompt(type, user) {
    var title = "";
    var imgName = "";
    var imgSize = "";

    if (type == "incorrect")
    {
        title = "Incorrect PIN";
        imgName = "sweet-alert-x.png";
        imgSize = "88x88";
    }
    else if (user == "habitat")
    {
        title = "Hello Habitat!";
        imgName = "habitat-logo.png";
        imgSize = "220x50";
    }
    else if (user == "appjaxx")
    {
        title = "Hola Josh";
        imgName = "appjaxx-nav.png"
        imgSize = "220x50";
    }

    swal({
        title: title,
        text: "Please enter your PIN to access this page",
        type: "input",
        inputType: "password",
        inputPlaceholder: "ENTER PIN",
        imageUrl: "../Assets/Images/" + imgName,
        imageSize: imgSize,
        allowEscapeKey: false,
        showCancelButton: false,
        confirmButtonColor: "#3fabe1",
        confirmButtonText: "Ok",
        customClass: "pinInput largeText",
        closeOnConfirm: false
    }, function (inputTxt) {
        console.log("Entered Text: [" + inputTxt + "]");

        if (inputTxt === false) return false;

        if (inputTxt === "")
        {
            swal.showInputError("Please enter a PIN.");
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


function submitPin(pin) {
    console.log("submitPin fired - PIN [" + pin + "]");

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
        url: "checkUsersPin",
        data: "{ 'user':'" + FROM +
              "', 'pin':'" + pin + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (result) {
            console.log("SUCCESS -> checkUsersPin result is... [next line]");
            console.log(result);

            if (result.success == true)
            {
                console.log("SubmitPIN: Success!");
                pinVerified = true;

                // THEN DISPLAY SUCCESS ALERT...
                swal({
                    title: "Access Granted",
                    text: "PIN successfully verified.",
                    type: "success",
                    customClass: "largeText",
                    showConfirmButton: false,
                    timer: 1500
                }, function () {
                    swal.close();

                    $('#pinBtnWrap').addClass('hidden');
                    $('#paymentForm').removeClass('hidden');

                    // CC (10/11/16): This only shows for Habitat, which only ever Sends payments.
                    $('#typeGrp #send').trigger("click");
                });
            }
            else if (result.msg != null)
            {
                console.log(result.msg);

                if (result.msg.indexOf("Incorrect") > -1)
                    showPinPrompt("incorrect", FROM);
                else if (result.msg.indexOf("User not found") > -1)
                {
                    console.log("Error: User not found");
                    showErrorAlert('2');
                }
                else
                    showErrorAlert('2');
            }
            else
                showErrorAlert('2');
        },
        Error: function (x, e) {
            // Hide the Loading Block
            $('#idWizContainer').unblock();

            console.log("Submit PIN ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('3');
        }
    });
}


function getSuggestedUsers() {
    $.ajax({
        type: "POST",
        url: "getUserSuggestions",
        data: "{'user':'" + FROM + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
            console.log(msg);

            if (typeof msg.suggestions != "undefined")
            {
                // Set up user Autocomplete
                $('#name').autocomplete({
                    lookup: msg.suggestions,
                    showNoSuggestionNotice: true,
                    noSuggestionNotice: "No users found :-(",
                    onSelect: function (suggestion) {
                        $('#email').val(suggestion.data.email);
                        //$('#memo').focus();
                    }
                });
            }
            return msg.suggestions;
        },
        Error: function (x, e) {
            console.log("ERROR --> 'x' then 'e' is... ");
            console.log(x);
            console.log(e);
        }
    });

    return false;
}


$(document).ajaxStop($.unblockUI);


function changeFavicon(src) {
    var link = document.createElement('link'),
     oldLink = document.getElementById('dynamic-favicon');
    link.id = 'dynamic-favicon';
    link.rel = 'shortcut icon';
    link.href = src;
    if (oldLink) document.head.removeChild(oldLink);
    document.head.appendChild(link);
}

$('body').on('focus', '.form-control', function () {
    $(this).closest('.fg-line').addClass('fg-toggled');
})

$('body').on('blur', '.form-control', function () {
    var fgrp = $(this).closest('.form-group');
    var ipgrp = $(this).closest('.input-group');

    var val = fgrp.find('.form-control').val();
    var val2 = ipgrp.find('.form-control').val();

    if (fgrp.hasClass('fg-float'))
    {
        if (val.length == 0)
            $(this).closest('.fg-line').removeClass('fg-toggled');
    }
    else if (ipgrp.hasClass('fg-float'))
    {
        if (val2.length == 0)
            $(this).closest('.fg-line').removeClass('fg-toggled');
    }
    else
        $(this).closest('.fg-line').removeClass('fg-toggled');
});