var TRANS_TYPE = $('#transType').val();
console.log("transType is: [" + TRANS_TYPE + "]");
var FOR_RENTSCENE = $('#rs').val();

// http://mathiasbynens.be/notes/document-head
document.head || (document.head = document.getElementsByTagName('head')[0]);

$(document).ready(function ()
{
    console.log("errorFromCodeBehind is: " + errorFromCodeBehind);

    if (FOR_RENTSCENE == "true") {
        $('.landingHeaderLogo').attr('href', 'http://www.rentscene.com');
        $('.landingHeaderLogo img').attr('src', '../Assets/Images/rentscene.png');
        $('.landingHeaderLogo img').attr('alt', 'Rent Scene Logo');

        if ($(window).width() > 1000) {
            $('.landingHeaderLogo img').css('width', '211px');
        }
		
		changeFavicon('../Assets/favicon2.ico')
    }

    if (areThereErrors() == false) {
        if (checkIfStillPending() == true) {
            //console.log(checkIfStillPending());

            var alertTitle = "";
            var alertBody = "";
            if (TRANS_TYPE == "request") {
                alertTitle = "Request Paid Successfully";
                alertBody = "<span>Your payment has been submitted successfully and is now being processed. &nbsp;You should see this payment appear on your bank statement within 1-3 business days.</span>" +
                            "<span style=\"display:block; margin-top: 14px;\">Please contact <a href=\"mailto:support@nooch.com\">Nooch Support</a> if you have any questions.</span>";
            }
            else {
                alertTitle = "Payment Accepted Successfully";
                alertBody = "<span>This payment has been submitted successfully and is now being processed. &nbsp;You should see this money appear on your bank statement within 2-4 business days.</span>" +
                            "<span style=\"display:block; margin-top: 14px;\">If you have any questions, please contact <a href=\"mailto:support@nooch.com\">Nooch Support</a> anytime.</span>";
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
        else {
            console.log("Transaction no longer pending!");
        }
    }
    else {
        console.log("59. There was an error! :-(");
    }

    // Format the Memo if present
    if ($("#transMemo").text().length > 0) {
        $("#transMemo").prepend("<i class='fa fa-fw fa-comment fa-flip-horizontal'>&nbsp;</i><em>&quot;</em>").append("<em>&quot;</em>");
    }
});

function areThereErrors()
{
    if (errorFromCodeBehind != '0') 
	{
        var alertTitle = "";
        var alertBodyText = "";
        var companyName = "Nooch";
        var supportEmail = "support@nooch.com";

        if (FOR_RENTSCENE == "true") {
            companyName = "Rent Scene"
            supportEmail = "payments@rentscene.com"
        }

        console.log('areThereErrors -> YES -> errorFromCodeBehind is: [' + errorFromCodeBehind + "]");

        if (errorFromCodeBehind == '1') {
            alertTitle = "Errors Are The Worst!";
            alertBodyText = "We had trouble finding that transaction. &nbsp;Please try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                            "<br/><a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
        }
        else if (errorFromCodeBehind == '2') {
            alertTitle = "Errors Are The Worst!";
            alertBodyText = "Terrible sorry, but it looks like we had trouble processing your data. &nbsp;Please refresh this page and try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
							companyName + " Support</span>:<br/><a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
        }
        else if (errorFromCodeBehind == 'failed') {
            alertTitle = "Errors Are Annoying";
            if (TRANS_TYPE == "request") {
                alertBodyText = "Our apologies, but we were not able to complete your payment request. &nbsp;Please refresh this page and try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
								companyName + " Support</span>:<br/><a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
            }
            else {
                alertBodyText = "Our apologies, but we were not able to deposit money in your account. &nbsp;Please refresh this page and try again and if you continue to see this message, contact <span style='font-weight:600;'>" +
								companyName + " Support</span>:<br/><a href='mailto:" + supportEmail + "' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>" + supportEmail + "</a>";
            }
        }
        else {
            alertTitle = "Errors are annoying";
            alertBodyText = "Very sorry about this, but we're having trouble processing your information, but the exact reason is not clear.  Please try again, or if this message persists, contact <a href='" + supportEmail + "' target='_blank'>" + supportEmail + "</a> for additional help.";
        }

        // Position the footer absolutely so it's at the bottom of the screen (it's normally pushed down by the body content)
        $('.footer').css({
            position: 'fixed',
            bottom: '3%'
        })

        swal({
            title: alertTitle,
            text: alertBodyText,
            type: "error",
            showCancelButton: true,
            confirmButtonColor: "#3fabe1",
            confirmButtonText: "OK",
            cancelButtonText: "Contact Support",
            closeOnConfirm: true,
            closeOnCancel: false,
            allowEscapeKey: false,
            html: true
        }, function (isConfirm) {
            if (!isConfirm) {
                if (FOR_RENTSCENE == "true") {
                    window.open("mailto:payments@rentscene.com");
                }
                else {
                    window.open("mailto:support@nooch.com");
                }
            }
        });

        return true;
    }

    console.log("No Errors!");
    return false;
}

function checkIfStillPending()
{
    if (isStillPending == false) // Set on Code Behind page
    {
        $("#depositInstructions").html('Looks like this request is no longer pending. &nbsp;Either you already responded by accepting or rejecting, or the sender cancelled it.');

        var bodyText = '<p>Looks like this payment request is no longer pending.</p>' +
                       '<p class=\"f-600 m-b-10\">This happened because either:</p>' +
                       '<div><span class="text-primary">•</span> &nbsp;You already responded by accepting or rejecting, or...<br/>' +
                       '<span class="text-primary">•</span> &nbsp;<span class=\"f-500\">' + $('#senderName1').text().trim() + '</span> cancelled it already.</div>';

        swal({
            title: "Request Expired",
            text: bodyText,
            type: "error",
            confirmButtonColor: "#3fabe1",
            confirmButtonText: "Ok",
            closeOnConfirm: true,
            allowEscapeKey: false,
            html: true
        });

        return false;
    }
    else // The transaction IS still pending
    {
        return true;
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