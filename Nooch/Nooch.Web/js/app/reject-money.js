var transType = $('#TransType').val();


$(document).ready(function () {

    console.log("errorFromCodeBehind is: " + errorFromCodeBehind);


    //$('#clickToReject').hide();
    //$('#createAccountPrompt').hide();

    if (areThereErrors() == false)
    {
        if (checkIfStillPending() == false)
        {
            console.log("Transaction no longer pending!");
        }

        if (transType == "Invite") 
        {
            $('#SenderAndTransInfodiv .intro-header').text('Payment From').addClass('label-success');
            $('#clickToReject p').text('Are you sure you want to reject this payment?');
        }
        else // must be a request
        {
            $('#SenderAndTransInfodiv .intro-header').text('Request From').addClass('label-danger');
            $('#clickToReject p').text('Are you sure you want to reject this payment request?');
        }
    }
    else 
    {
        console.log("20. There was an error! :-(");
    }

    // Format the Memo if present
    if ($("#transMemo").text().length > 0) {
        $("#transMemo").prepend("<i class='fa fa-fw fa-comment fa-flip-horizontal'>&nbsp;</i><em>&quot;</em>").append("<em>&quot;</em>");
    }
});

function areThereErrors() {

    if (errorFromCodeBehind != '0')
    {
        var alertTitle = "";
        var alertBodyText = "";

        console.log('areThereErrors -> errorFromCodeBehind is: [' + errorFromCodeBehind + "]");

        showErrorAlert(errorFromCodeBehind)

        // Position the footer absolutely so it's at the bottom of the screen (it's normally pushed down by the body content)
        $('.footer').css({
            position: 'fixed',
            bottom: '5%'
        })

        return true;
    }

    console.log("No Errors!");
	return false;
}

function checkIfStillPending() 
{
    if (typeof transStatus != 'undefined' &&
        transStatus != "pending") // Set on Code Behind page
    {
        var alertTitle = "";
        var alertBodyText = "";

        $('#SenderAndTransInfodiv').hide();
        $('#clickToReject').hide();
        $('#createAccountPrompt').hide();
        if (transStatus == "success")
        {
          
            alertTitle = "Payment Already Completed";
            alertBodyText = "Looks like this payment was already paid successfully.";
        }
        else if (transStatus == "cancelled")
        {
            
            alertTitle = "Payment Already Cancelled";
            alertBodyText = "Looks like " + $('#nameLabel').text() + " already cancelled this payment.&nbsp; You're off the hook!";
        }
        else if (transStatus == "rejected")
        {
          
            alertTitle = "Payment Already Rejected";
            alertBodyText = "Looks like you already rejected this payment.";
        }
        else
        {
            alertTitle = "Payment Expired";
            alertBodyText = "Looks like this payment request is no longer pending.&nbsp; You're off the hook!";
        }

        alertBodyText += "<div class=\"m-15\">Please contact <span style='font-weight:600;'>Nooch Support</span> if you believe this is an error.<br/>" +
                         "<a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a></div>";

        $("#fromText").html(alertBodyText).addClass('errorMessage');
        $(".errorMessage a").addClass("btn btn-default m-t-20 animated bounceIn");

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
                window.open("mailto:support@nooch.com");
            }
        });

        return false;
    }
    else // The transaction IS still pending
    {
        return true;
    }
}


function rejectThisRequest() {
    /*console.log("rejectThisRequestReached!");

    // ADD THE LOADING BOX
    $('#body-depositNew').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Rejecting This Request...</span>',
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

    var transId = getParameterByName('TransactionId');
    var userType = getParameterByName('UserType');
    var linkSource = getParameterByName('LinkSource');
    var transType = getParameterByName('TransType');

    $.ajax({
        type: "POST",
        url: "reject-money.aspx/RejectThisRequest",
        data: "{'TransId':'" + transId +
              "', 'UserType':'" + userType +
              "', 'LinkSource':'" + linkSource +
              "', 'TransType':'" + transType + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {

            console.log("SUCCESS -> 'RegisterUserWithSynpResult' is... ");
            console.log(msg);
            console.log(msg.d);

            // Hide the Loading Block
            $('#body-depositNew').unblock();

            if (msg = "success")
            {
                $("#fromText").html("Request Rejected Successfully");

                swal({
                    title: "No Problem",
                    text: "You just rejected that request successfully.<br/><br/>Hope " + $('#nameLabel').text().trim() + " won't mind!",
                    type: "success",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "OK",
                    closeOnConfirm: true,
                    customClass: "largeText",
                    html: true
                });
            }
            else
            {
                console.log("Response was not successful :-(");
                showErrorAlert('3');
            }
        },
        Error: function (x, e) {
            // Hide the Loading Block
            $('#body-depositNew').unblock();

            console.log("ERROR --> 'x', then 'e' is... ");
            console.log(x);
            console.log(e);

            showErrorAlert('3');
        }
    });*/
}


function showErrorAlert(errorNum) {
    var alertTitle = "";
    var alertBodyText = "";

    console.log("ShowError -> errorNum is: [" + errorNum + "]");

    if (errorNum == '1')
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "We had trouble finding that transaction.  Please try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";


        $('#SenderAndTransInfodiv').hide();
        $('#clickToReject').hide();
        $('#createAccountPrompt').hide();
        

    }
    else if (errorNum == '2')
    {
        alertTitle = "Errors Are Annoying";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing your data.  Please refresh this page and try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
        $('#SenderAndTransInfodiv').hide();
        $('#clickToReject').hide();
        $('#createAccountPrompt').hide();

    }
    else if (errorNum == '3')
    {
        $('#SenderAndTransInfodiv').hide();
        $('#clickToReject').hide();
        $('#createAccountPrompt').hide();

        alertTitle = "Errors Are Annoying";
        alertBodyText = "Our apologies, but we were not able to reject that request.  Please try again or contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }
    // Generic Error
    else
    {
        alertTitle = "Errors Are The Worst!";
        alertBodyText = "Terrible sorry, but it looks like we had trouble processing your request.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                        "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
    }


    $("#fromText").html(alertBodyText).addClass('errorMessage');
    $(".errorMessage a").addClass("btn btn-default m-t-20 animated bounceIn");

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
    });

}

function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);
    return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}
