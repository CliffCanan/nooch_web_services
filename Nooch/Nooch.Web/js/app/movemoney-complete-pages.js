var TRANS_TYPE = $('#transType').val();
var COMPANY_VAL = $('#company').val();
var COMPANY_FORMATTED = "";
var SUPPORT_EMAIL = "";
if (COMPANY_VAL == "rentscene")
{
    COMPANY_FORMATTED = "Rent Scene";
    SUPPORT_EMAIL = "payments@rentscene.com";
}
else if (COMPANY_VAL == "habitat")
{
    COMPANY_FORMATTED = "Habitat";
    SUPPORT_EMAIL = "support@nooch.com";
}
else
{
    COMPANY_FORMATTED = "Nooch";
    SUPPORT_EMAIL = "support@nooch.com";
}

var ERROR_MSG = $('#errorMsg').val();
var STILL_PENDING = $('#isStillPending').val();

// http://mathiasbynens.be/notes/document-head
document.head || (document.head = document.getElementsByTagName('head')[0]);

$(document).ready(function () {
    console.log("ERROR_MSG is: [" + ERROR_MSG + "]");

    if (COMPANY_VAL == "rentscene")
    {
        if ($(window).width() > 1000) $('.landingHeaderLogo img').css('width', '170px');

        document.title = TRANS_TYPE == "request" ? "Request Paid | Rent Scene Payments" : "Payment Accepted | Rent Scene Payments"
        changeFavicon('../Assets/favicon2.ico');
    }
    else if (COMPANY_VAL == "habitat")
    {
        document.title = TRANS_TYPE == "request" ? "Request Paid | Habitat Payments" : "Payment Accepted | Habitat Payments"
        changeFavicon('../Assets/favicon-habitat.png')
    }


    if (areThereErrors() == false)
    {
        if (checkIfStillPending() == true)
        {
            var alertTitle = "";
            var alertBody = "";
            if (TRANS_TYPE == "request")
            {
                alertTitle = "Request Paid Successfully";
                alertBody = "<span>Your payment has been submitted successfully and is now being processed. &nbsp;You should see this payment appear on your bank statement within 1-3 business days.</span>" +
                            "<span style=\"display:block; margin-top: 14px;\">Please contact <a href='mailto:" + SUPPORT_EMAIL + "'>" + COMPANY_FORMATTED + " Support</a> if you have any questions.</span>";
            }
            else
            {
                alertTitle = "Payment Accepted Successfully";
                alertBody = "<span>This payment has been submitted successfully and is now being processed. &nbsp;You should see this money appear on your bank statement within 2-4 business days.</span>" +
                            "<span style=\"display:block; margin-top: 14px;\">If you have any questions, please contact <a href='mailto:" + SUPPORT_EMAIL + "'>" + COMPANY_FORMATTED + " Support</a> anytime.</span>";
            }

            swal({
                title: alertTitle,
                text: alertBody,
                type: "success",
                confirmButtonColor: "#3fabe1",
                confirmButtonText: "Awesome",
                allowEscapeKey: false,
                html: true
            });
        }
        else console.log("Transaction no longer pending!");
    }
    else console.log("54. There was an error! :-(");

    // Format the Memo if present
    if ($("#transMemo").text().length > 0)
        $("#transMemo").prepend("<i class='fa fa-fw fa-comment fa-flip-horizontal'>&nbsp;</i><em>&quot;</em>").append("<em>&quot;</em>");
});

function areThereErrors() {
    if (ERROR_MSG != 'ok')
    {
        var alertTitle = "";
        var alertBodyText = "";

        console.log('areThereErrors -> YES -> errorFromCodeBehind is: [' + errorFromCodeBehind + "]");

        if (errorFromCodeBehind == "1")
        {
            alertTitle = "Errors Are The Worst!";
            alertBodyText = "We had trouble finding that transaction. &nbsp;Please try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                            "<br/><a href='mailto:" + SUPPORT_EMAIL + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + SUPPORT_EMAIL + "</a>";
        }
        else if (errorFromCodeBehind == "2")
        {
            alertTitle = "Errors Are The Worst!";
            alertBodyText = "Terrible sorry, but it looks like we had trouble processing your data. &nbsp;Please refresh this page and try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
							COMPANY_FORMATTED + " Support</span>:<br/><a href='mailto:" + SUPPORT_EMAIL + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + SUPPORT_EMAIL + "</a>";
        }
        else if (errorFromCodeBehind == "failed")
        {
            alertTitle = "Errors Are Annoying";
            if (TRANS_TYPE == "request")
                alertBodyText = "Our apologies, but we were not able to complete your payment. &nbsp;Please refresh this page and try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
								COMPANY_FORMATTED + " Support</span>:<br/><a href='mailto:" + SUPPORT_EMAIL + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + SUPPORT_EMAIL + "</a>";
            else
                alertBodyText = "Our apologies, but we were not able to deposit money in your account. &nbsp;Please refresh this page and try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
								COMPANY_FORMATTED + " Support</span>:<br/><a href='mailto:" + SUPPORT_EMAIL + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + SUPPORT_EMAIL + "</a>";
        }
        else
        {
            alertTitle = "Errors are annoying";
            alertBodyText = "Very sorry about this, but we're having trouble processing your information, but the exact reason is not clear. " +
                            "Please try again, or if this message persists, contact <a href='" + SUPPORT_EMAIL + "' target='_blank'>" + SUPPORT_EMAIL + "</a> for additional help.";
        }

        // Position the footer absolutely so it's at the bottom of the screen (it's normally pushed down by the body content)
        $('.footer').css({
            position: 'fixed',
            bottom: '1%'
        })

        swal({
            title: alertTitle,
            text: alertBodyText,
            type: "error",
            showCancelButton: true,
            confirmButtonColor: "#3fabe1",
            confirmButtonText: "OK",
            cancelButtonText: "Contact Support",
            closeOnCancel: false,
            allowEscapeKey: false,
            html: true
        }, function (isConfirm) {
            if (!isConfirm)
                window.open("mailto:" + SUPPORT_EMAIL);
        });

        return true;
    }

    console.log("No Errors!");
    return false;
}

function checkIfStillPending() {
    if (STILL_PENDING == "false")
    {
        $("#depositInstructions").html('Looks like this request is no longer pending. &nbsp;Either you already responded by accepting or rejecting, or the sender cancelled it.');

        var bodyText = '<p>Looks like this payment is no longer pending.</p>' +
                       '<p class=\"f-600 m-b-10\">This happened because either:</p>' +
                       '<div><span class="text-primary">•</span> &nbsp;You already responded by accepting or rejecting, or...<br/>' +
                       '<span class="text-primary">•</span> &nbsp;<span class=\"f-500\">' + $('#senderName1').text().trim() + '</span> cancelled it already.</div>';

        swal({
            title: "Payment Expired",
            text: bodyText,
            type: "error",
            confirmButtonColor: "#3fabe1",
            confirmButtonText: "Ok",
            allowEscapeKey: false,
            html: true
        });

        return false;
    }
    else // The transaction IS still pending
        return true;
}

function changeFavicon(src) {
    var link = document.createElement('link'),
     oldLink = document.getElementById('dynamic-favicon');
    link.id = 'dynamic-favicon';
    link.rel = 'shortcut icon';
    link.href = src;
    if (oldLink) document.head.removeChild(oldLink);
    document.head.appendChild(link);
}