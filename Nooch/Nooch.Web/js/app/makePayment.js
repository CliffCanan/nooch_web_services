var errorId = $('#errorId').val();
var RENTSCENE = $('#rs').val();
var COMPANY = "Nooch";

var isRequest;
var amount, name, email, memo, pin, ipVal;

var existNAME, existMEMID;

$(document).ready(function ()
{
    var isSmScrn = false;
    if ($(window).width() < 768)
    {
        isSmScrn = true;
    }

    if (RENTSCENE == "yes")
    {
        COMPANY = "Rent Scene";

        $('.navbar img').attr('href', 'http://www.rentscene.com');

        if (isSmScrn)
        {
            $('.navbar img').css('width', '130px');
        }
    }
    else
    {
        $('.navbar img').attr('src', '../Assets/Images/nooch-logo2.svg');
    }

    $('#amount').mask("#,##0.00", { reverse: true });

    setTimeout(function ()
    {
        $('#amount').focus();
    }, 200)

    //console.log(ipusr);

    $('[data-toggle="popover"]').popover();

    $('#submitPayment').click(function ()
    {
        checkFormData();
        return false;
    });
});

function formatAmount()
{
	var formattedAmount = $('#amount').val().trim().replace(",","");

	if (formattedAmount.length > 0) {
		if (formattedAmount.length <= 2) {
			$('#amount').val(formattedAmount + '.00');
		}

		if (Number(formattedAmount) < 5 || Number(formattedAmount) > 5000) {
			updateValidationUi("amount", false);
		}
		else {
			updateValidationUi("amount", true);
		}
	}
	else {
		updateValidationUi("amount", false);
	}
}

function checkFormData()
{
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
                    else
                    {
                        updateValidationUi("memo", false);
                    }
                }
                else
                {
                    updateValidationUi("email", false);
                }
            }
            else
            {
                updateValidationUi("name", false);
            }
        }
        else
        {
            updateValidationUi("name", false);
        }
    }
    else
    {
        updateValidationUi("amount", false);
    }

    return;
}


function submitPayment()
{
    // ADD THE LOADING BOX
    $('#makePaymentContainer').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Submitting Payment...</span>',
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

    isRequest = true;
    amount = $('#amount').val();
    name = $('#name').val();
    email = $('#email').val().trim();
    memo = $('#memo').val().trim();
    pin = "";
    ipVal = ipusr;

    console.log("SUBMIT PAYMENT -> {isRequest: " + isRequest +
                                 ", amount: " + amount +
                                 ", name: " + name +
                                 ", email: " + email +
                                 ", memo: " + memo +
                                 ", pin: " + pin +
                                 ", ipVal: " + ipVal + "}");

    $.ajax({
        type: "POST",
        url: URLs.submitPayment,
        data: "{'isRequest':'" + isRequest +
              "', 'amount':'" + amount +
              "', 'name':'" + name +
              "', 'email':'" + email +
              "', 'memo':'" + memo +
              "', 'pin':'" + pin +
              "', 'ip':'" + ipVal + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg)
        {
            var sendPaymentResponse = msg;
            console.log("SUCCESS -> 'sendPaymentResponse' is... ");
            console.log(sendPaymentResponse);

            resultReason = sendPaymentResponse.msg;

            // Hide the Loading Block
            $('#makePaymentContainer').unblock();

            if (sendPaymentResponse.success == true)
            {
                if (sendPaymentResponse.isEmailAlreadyReg == true)
                {
                    existNAME = sendPaymentResponse.name;
                    existMEMID = sendPaymentResponse.memberId;

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

                    var bodyText = "<table border='0' width='95%' cellpadding='4' style='font-size: 16px; margin:10px auto 12px;'><tbody>" +
                                   "<tr><td style='vertical-align:top'>Name:</td><td><strong>" + sendPaymentResponse.name + "</strong></br><small>" + email + "</small></strong></td></tr>" +
                                   "<tr><td>Status:</td><td><span class='" + statusCssClass + "'>" + status + "</span></td></tr>" +
                                   "<tr><td>Date Created:</td><td>" + sendPaymentResponse.dateCreated + "</td></tr>" +
                                   "<tr><td>Bank Linked:</td><td>" + sendPaymentResponse.isBankAttached + "</td></tr>";
                    
                    if (sendPaymentResponse.isBankAttached == true)
                        bodyText = bodyText + "<tr><td>Bank Status:</td><td>" + sendPaymentResponse.bankStatus + "</td></tr>";
                    
                    bodyText = bodyText + "</tbody></table>";

                    // THEN DISPLAY SUCCESS ALERT...
                    swal({
                        title: "Email Already Registered",
                        text: "The following user already is registered:" +
                              bodyText + 
                              "<span class='show f-600'>Do you still want to send a payment request to " + sendPaymentResponse.name + "?</span>",
                        type: "warning",
                        showCancelButton: true,
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Send",
                        html: true
                    }, function (isConfirm)
                    {
                        if (isConfirm)
                        {
                            sendRequestToExistingUser();
                        }
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
                              "<tr><td>Memo</td><td>" + memo + "</td></tr></tbody></table>",
                        type: "success",
                        showCancelButton: false,
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Ok",
                        html: true
                    }, function ()
                    {
                        resetForm();
                    });
                }
            }
            else
            {
                if (resultReason != null)
                {
                    if (resultReason.indexOf("email already registered") > -1)
                    {
                        showErrorAlert('20');
                    }
					else if (resultReason.indexOf("Requester does not have any verified bank account") > -1)
					{
						showErrorAlert('4');
					}
					else if (resultReason.indexOf("Missing") > -1)
					{
						showErrorAlert('5');
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
        Error: function (x, e)
        {
            // Hide the Loading Block
            $('#makePaymentContainer').unblock();

            console.log("ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('2');
        }
    });
}


function sendRequestToExistingUser()
{
    // ADD THE LOADING BOX
    $('#makePaymentContainer').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Submitting Payment...</span>',
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

    console.log("SUBMIT PAYMENT (2nd time - to existing user) -> {isRequest: " + isRequest +
                                 ", amount: " + amount +
                                 ", nameEntered: " + name +
                                 ", nameFromServer: " + existNAME+
                                 ", email: " + email +
                                 ", memo: " + memo +
                                 ", MemID: " + existMEMID +
                                 ", pin: " + pin +
                                 ", ipVal: " + ipVal + "}");


    $.ajax({
        type: "POST",
        url: URLs.submitRequestToExistingUser,
        data: "{'isRequest':'" + isRequest +
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
        success: function (msg)
        {
            var sendPaymentResponse = msg;
            console.log("SUCCESS -> 'sendPaymentResponse' is... ");
            console.log(sendPaymentResponse);

            resultReason = sendPaymentResponse;

            // Hide the Loading Block
            $('#makePaymentContainer').unblock();

            if (sendPaymentResponse.success == true)
            {
                $('.alert.alert-success #resultAmount').text('$' + amount);
                $('.alert.alert-success #resultName').text(existNAME);
                $('.alert.alert-success').removeClass('hidden').slideDown();

                // THEN DISPLAY SUCCESS ALERT...
                swal({
                    title: "Payment Created Successfully",
                    text: "<table border='0' width='95%' cellpadding='8'><tbody>" +
                          "<tr><td>Amount</td><td>$" + amount + "</td></tr>" +
                          "<tr><td>Name</td><td>" + existNAME + "</td></tr>" +
                          "<tr><td>Email</td><td>" + email + "</td></tr>" +
                          "<tr><td>Memo</td><td>" + memo + "</td></tr></tbody></table>",
                    type: "success",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Ok",
                    html: true
                }, function ()
                {
                    resetForm();
                });
            }
            else
            {
                if (resultReason != null)
                {
                    showErrorAlert('2');
                }
                else
                {
                    showErrorAlert('2');
                }
            }
        },
        Error: function (x, e)
        {
            // Hide the Loading Block
            $('#makePaymentContainer').unblock();

            console.log("ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('2');
        }
    });
}


function updateValidationUi(field, success)
{
    //console.log("Field: " + field + "; success: " + success);

    if (success == true)
    {
        $('#' + field + 'Grp').removeClass('has-error').addClass('has-success');
        $('#' + field + 'Grp .help-block').slideUp();

        // Show the success checkmark
        if (!$('#' + field + 'Grp .iconFeedback').length)
        {
            $('#' + field + 'Grp .fg-line').append('<i class="fa fa-check text-success iconFeedback animated bounceIn"></i>');
        }
        else
        {
            $('#' + field + 'Grp .iconFeedback').removeClass('bounceOut').addClass('bounceIn');
        }

        if (field == "name")
            $('#name').addClass('capitalize');
    }
    else
    {
        $('#' + field + 'Grp').removeClass('has-success').addClass('has-error');

        // Hide the success checkmark if present
        if ($('#' + field + 'Grp .iconFeedback').length)
        {
            $('#' + field + 'Grp .iconFeedback').addClass('bounceOut');
        }

        var helpBlockTxt = "";
        if (field == "amount")
        {
            helpBlockTxt = "Please enter an amount between <strong>$5 - $5,000</strong>.";
        }
        else if (field == "name")
        {
            helpBlockTxt = "Please enter a name for this recipient."
        }
        else if (field == "email")
        {
            helpBlockTxt = "Please enter a valid email."
        }
        else if (field == "memo")
        {
            helpBlockTxt = "Please enter a descriptive memo so that the recipient knows what this payment is for."
        }

        if (!$('#' + field + 'Grp .inputContainer .help-block').length)
        {
            $('#' + field + 'Grp .inputContainer').append('<small class="help-block" style="display:none">' + helpBlockTxt + '</small>');
            $('#' + field + 'Grp .inputContainer .help-block').slideDown();
        }
        else
        {
            $('#' + field + 'Grp .inputContainer .help-block').show()
        }

        // Now focus on the element that failed validation
        setTimeout(function ()
        {
            $('#' + field + 'Grp input').focus();
        }, 200)
    }
}


function ValidateEmail(str)
{
    var at = "@"
    var dot = "."
    var lat = str.indexOf(at)
    var lstr = str.length
    var ldot = str.indexOf(dot)

    if (lstr < 5)
    {
        return false;
    }

    if (lat == -1 || lat == 0 || lat == lstr)
    {
        return false
    }

    if (ldot == -1 || ldot == 0 || ldot > lstr - 3)
    {
        return false
    }

    if (str.indexOf(at, (lat + 1)) != -1)
    {
        return false
    }

    if (str.substring(lat - 1, lat) == dot || str.substring(lat + 1, lat + 2) == dot)
    {
        return false
    }

    if (str.indexOf(dot, (lat + 2)) == -1)
    {
        return false
    }

    if (str.indexOf(" ") != -1)
    {
        return false
    }

    return true
};


function resetForm()
{
    console.log("Resetting form...");

    $('#paymentForm .form-group').removeClass('has-error has-success');
    $('.iconFeedback').addClass('bounceOut');
    $('.help-block').slideUp();

    $('#amount').val('');
    $('#name').val('');
    $('#email').val('');
    $('#memo').val('');
}


function showErrorAlert(errorNum)
{
    var alertTitle = "";
    var alertBodyText = "";
    var shouldFocusOnEmail = false;
    var shouldShowErrorDiv = true;

    console.log("ShowError -> errorNum is: [" + errorNum + "], resultReason is: [" + resultReason + "]");

    if (errorNum == '1') // Codebehind errors
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "We had trouble finding that transaction.  Please try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '2' || errorNum == '3') // Errors after submitting ID verification AJAX
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing your info. &nbsp;Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '4') // Requester does not have any verified bank account
    {
        alertTitle = "Oh No - Error!";
        alertBodyText = "Looks like the requester's account (yours) does not have a verified bank attached.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '5') // Missing some piece of data (shouldn't ever happen b/c the form shouldn't submit if missing anything)
    {
        alertTitle = "Oh No - Error!";
        alertBodyText = "Looks like we were missing some required information. &nbsp;Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    else if (errorNum == '20') // EMAIL came back as already registered with Nooch.
    {
        alertTitle = "Email Already Registered";
        alertBodyText = "Looks like <strong>" + $('#idVer-email').val() + "</strong> is already registered to a Nooch account. &nbsp;Please try a different email address.";
        shouldFocusOnEmail = true;
        shouldShowErrorDiv = false;
    }
    else // Generic Error
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing that request. &nbsp;Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support:</span>" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
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
        confirmButtonText: "Ok  :-(",
        cancelButtonText: "Contact Support",
        closeOnConfirm: true,
        closeOnCancel: false,
        allowEscapeKey: false,
        html: true
    }, function (isConfirm)
    {
        if (!isConfirm)
        {
            window.open("mailto:support@nooch.com");
        }
        else if (shouldFocusOnEmail)
        {
            updateValidationUi("email", false);
        }
    });
}


$('body').on('focus', '.form-control', function ()
{
    $(this).closest('.fg-line').addClass('fg-toggled');
})

$('body').on('blur', '.form-control', function ()
{
    var fgrp = $(this).closest('.form-group');
    var ipgrp = $(this).closest('.input-group');

    var val = fgrp.find('.form-control').val();
    var val2 = ipgrp.find('.form-control').val();

    if (fgrp.hasClass('fg-float'))
    {
        if (val.length == 0)
        {
            $(this).closest('.fg-line').removeClass('fg-toggled');
        }
    }
    else if (ipgrp.hasClass('fg-float'))
    {
        if (val2.length == 0)
        {
            $(this).closest('.fg-line').removeClass('fg-toggled');
        }
    }
    else
    {
        $(this).closest('.fg-line').removeClass('fg-toggled');
    }
});