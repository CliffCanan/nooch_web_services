console.log("idverification file reached");

var memid = $("#memid").val();
var was_error = $("#was_error").val();
var error_msg = $("#error_msg").val();
var from = $("#from").val();
var RED_URL = $("#redUrl").val();
var isCompleted = false;

console.log("ID Ver Loaded for: [" + memid +"]");
console.log("FROM: [" + from + "]");
console.log("RED_URL: [" + RED_URL + "]");

$(document).ready(function() {	

    if (areThereErrors() == false)
    {
        $("#idVerWizard").steps({
            headerTag: "h3",
            bodyTag: "section",
            transitionEffect: 'slideLeft',
            transitionEffectSpeed: 400,

            /* Events */
            onInit: function (event, curretIndex) {
            },
            onStepChanging: function (event, currentIndex, newIndex) {

                if (newIndex == 0) {
                    $('#questionNum').text('1');
                }

                // IF going to Step 2
                if (newIndex == 1) {
                    if ($('input[name=question1]:checked').length > 0) {
                        updateValidationUi(1, true);

                        $('#questionNum').text('2');
                        return true;
                    }
                    else {
                        updateValidationUi(1, false);
                        return false;
                    }
                }

                // IF going to Step 3
                if (newIndex == 2) {
                    if ($('input[name=question2]:checked').length > 0) {
                        updateValidationUi(2, true);

                        $('#questionNum').text('3');
                        return true;
                    }
                    else {
                        updateValidationUi(2, false);
                        return false;
                    }
                }

                // IF going to Step 4
                if (newIndex == 3) {
                    if ($('input[name=question3]:checked').length > 0) {
                        updateValidationUi(3, true);

                        $('#questionNum').text('4');
                        return true;
                    }
                    else {
                        updateValidationUi(3, false);
                        return false;
                    }
                }

                // IF going to Step 5
                if (newIndex == 4) {
                    if ($('input[name=question4]:checked').length > 0) {
                        updateValidationUi(4, true);

                        $('#questionNum').text('5');
                        return true;
                    }
                    else {
                        updateValidationUi(4, false);
                        return false;
                    }
                }

                // Always allow previous action even if the current form is not valid!
                if (currentIndex > newIndex) {
                    return true;
                }
            },
            onStepChanged: function (event, currentIndex, priorIndex) {
            },
            onCanceled: function (event) {
                swal({
                    title: "Cancel?",
                    text: "Are you sure you want to cancel verifying your identity?  You can come back any time and do this later.",
                    type: "warning",
                    showCancelButton: true,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Yes - Cancel"
                }, function (isConfirm) {
                    if (isConfirm) {

                    }
                });
            },
            onFinishing: function (event, currentIndex) {
                // CHECK TO MAKE SURE ALL FIELDS WERE COMPLETED
                if (currentIndex == 4) {
                    if ($('input[name=question5]:checked').length > 0)
                    {
                        updateValidationUi(5, true);
                        return true;
                    }
                    else
                    {
                        updateValidationUi(5, false);
                        return false;
                    }
                }
            },
            onFinished: function (event, currentIndex) {
                submitAnswers()
            }
        });

        $("#idVerWizard").removeClass("hidden");

        updateValidationUi = function (step, success) {
            var group = "#q" + step + "Grp";

            if (success == true) {
                resetErrorAlert(step);
            }
            else {
                if ($('#step' + step + 'Feedback .alert').hasClass('hidden') == true) {
                    $('.wizard > .content').animate({ height: "27.25em" }, 600, "easeOutCubic");
                    $('#step' + step + 'Feedback + .form-group').css("margin-top", "-30px");
                    $('#step' + step + 'Feedback + .form-group').animate({ "margin-top": "0px" }, 600, "easeOutCubic");
                    $('#step' + step + 'Feedback .alert').removeClass('zoomOut').addClass('bounceIn').removeClass('hidden');
                }
            }
        }

        resetErrorAlert = function (step) {
            if ($('#step' + step + 'Feedback .alert').hasClass('hidden') == false) {
                $('.wizard > .content').animate({ height: "26em" }, 600, "easeInOutQuad");
                $('#step' + step + 'Feedback .alert').removeClass('bounceIn').addClass('zoomOut');

                setTimeout(function () {
                    $('#step' + step + 'Feedback .alert').removeClass('zoomOut').addClass('hidden')
                }, 720);
            }
        }
    }
    else
    {
        console.log("161. There was an error: [" + error_msg + "] :-(");
    }
});


function submitAnswers() {

    if (from == "lndngpg")
    {
        console.log("TRIGGERING addblockLdg IN PARENT");
        window.parent.$('body').trigger('addblockLdg');
    }
    else
    {
        $('.idVerify-container').block({
            message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Submitting responses...</span>',
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

    var answer1 = $('input[name="question1"]:checked').val();
    var answer2 = $('input[name="question2"]:checked').val();
    var answer3 = $('input[name="question3"]:checked').val();
    var answer4 = $('input[name="question4"]:checked').val();
    var answer5 = $('input[name="question5"]:checked').val();

    console.log("Submitting answers... " +
                "{'memId':'" + memid +
              "', 'qSetId':'" + $('#qsetId').val() +
              "', 'a1':'" + answer1 +
              "', 'a2':'" + answer2 +
              "', 'a3':'" + answer3 +
              "', 'a4':'" + answer4 +
              "', 'a5':'" + answer5 + "'}");

    $.ajax({
        type: "POST",
        url: "idverification.aspx/submitResponses",
        data: "{'memId':'" + memid +
              "', 'qSetId':'" + $('#qsetId').val() +
              "', 'a1':'" + answer1 +
              "', 'a2':'" + answer2 +
              "', 'a3':'" + answer3 +
              "', 'a4':'" + answer4 +
              "', 'a5':'" + answer5 + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (response) {
            console.log("Ajax SUCCESS reached");
            console.log(response);

            var submitAnswersResponse = response.d;

            if (submitAnswersResponse.isSuccess.toString() == "true")
            {
                isCompleted = true;

                console.log("AJAX Success - 'FROM' is: " + from);

                if (from == "lndngpg") // FROM NON-NOOCH USER LANDING PAGE
                {
                    console.log("TRIGGERING COMPLETE IN PARENT");
                    window.parent.$('body').trigger('complete');
                }
                else if (from == "llapp") // FROM LANDLORD APP
                {
                    console.log("TRIGGERING addblockLdg IN LL APP");
                    window.parent.$('body').trigger('ExraIdVerComplete');
                }
                else if (RED_URL.indexOf("rentscene") > -1) // For RentScene
                {
                    $('.idVerify-container').unblock();

                    swal({
                        title: "Great Success",
                        text: "<p>Bank linked successfully!</p><p>We'll be in touch in the next few days about how to get started using Nooch for rent payments.</p>",
                        type: "success",
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Awesome",
                        customClass: "largeText",
                        html: true
                    }, function (isConfirm) {
                        displayLoadingBox("Finishing...");

                        setTimeout(function () {
                            window.top.location.href = "https://www.nooch.com/nooch-for-landlords";
                        }, 750);
                    });
                }
                else
                {
                    console.log("TRIGGERING local COMPLETE");
                    $('.idVerify-container').unblock();

                    // THEN DISPLAY SUCCESS ALERT...
                    swal({
                        title: "Great Job!",
                        text: "<i class=\"mdi mdi-account-check text-success\"></i><br/>Thanks for submitting your ID information. That helps us keep Nooch safe for everyone.",
                        type: "success",
                        showCancelButton: false,
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Great!",
                        closeOnConfirm: true,
                        html: true,
                        customClass: "idVerSuccessAlert"
                    }, function (isConfirm) {
                        displayLoadingBox("Finishing...");

                        setTimeout(function () {
                            window.top.location.href = RED_URL;
                        }, 500);
                    });
                }
            }
            else // ERROR CAME BACK FROM SERVER
            {
                console.log("Submit Verification Answers ERROR is: " + submitAnswersResponse.msg);

                isCompleted = true; //(see note in error handler)

                if (from == "lndngpg")
                {
                    window.parent.$('body').trigger('complete');
                }
                else if (from == "llapp") // FROM LANDLORD APP
                {
                    console.log("TRIGGERING addblockLdg IN LL APP");
                    window.parent.$('body').trigger('ExraIdVerComplete');
                }
                else
                {
                    $('.idVerify-container').unblock();
                    displayLoadingBox("Finishing...");
                    setTimeout(function () {
                        window.top.location.href = RED_URL;
                    }, 500);
                }
            }
        },
        Error: function (x, e) {
            console.log("Ajax Error reached");
            // (9.29/15) Setting to true even in case of error so that the User can continue adding a bank.
            //           We can re-ask the verification questions later, but don't want to lose the user entirely.
            isCompleted = true;
            if (from == "lndngpg")
            {
                window.parent.$('body').trigger('complete');
            }
            else if (from == "llapp") // FROM LANDLORD APP
            {
                console.log("TRIGGERING addblockLdg IN LL APP");
                window.parent.$('body').trigger('ExraIdVerComplete');
            }
            else
            {
                showErrorAlert('2');
            }
        }
    });
}


function displayLoadingBox(text) {
    $('.idVerify-container').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">' + text + '</span>',
        css: {
            border: 'none',
            padding: '20px 8px 14px',
            backgroundColor: '#000',
            '-webkit-border-radius': '12px',
            '-moz-border-radius': '12px',
            'border-radius': '12px',
            opacity: '.7',
            width: '80%',
            left: '10%',
            top: '25px',
            color: '#fff'
        }
    });
}


function areThereErrors() {

    if (was_error != 'false') // could be 'initial' or 'true' if there were any errors
    {
        console.log('areThereErrors -> error_msg is: [' + error_msg + "]");

        // Handle Errors: If coming from another page via iFrame, trigger error in parent window
        //                Otherwise, show error on this page.
        if (from == "lndngpg")
        {
            if (error_msg == "Answers already submitted for this user")
            {
                isCompleted = true;
            }
            else
            {
                isCompleted = false;
            }
            window.parent.$('body').trigger('complete');
        }
        else if (error_msg == "Answers already submitted for this user")
        {
            swal({
                title: "ID Already Verified",
                text: "",
                type: "success",
                confirmButtonColor: "#3fabe1",
                confirmButtonText: "OK",
                allowEscapeKey: false
            }, function () {
                window.top.location.href = RED_URL;
            });
        }
        else
        {
            showErrorAlert('1');
        }

        return true;
    }

    console.log("ID Ver JS - No Errors!");
    return false;
}


function showErrorAlert(errorNum) {
    if (from == "lndngpg")
    {
        isCompleted = false;
        window.parent.$('body').trigger('complete');
    }
    else
    {
        var alertTitle = "";
        var alertBodyText = "";
        var shouldShowErrorDiv = true;

        console.log('ShowError -> errorNum is: [" + errorNum + "], error_msg is: [' + error_msg + "]");

        if (errorNum == '1' || errorNum == '2') // CodeBeind Errors, or Errors after submitting ID verification AJAX
        {
            alertTitle = "Errors Are The Worst!";
            alertBodyText = "Terrible sorry, but it looks like we had trouble processing your info.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                            "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
        }
        else // Generic Error
        {
            alertTitle = "Errors Are The Worst!";
            alertBodyText = "Terrible sorry, but it looks like we had trouble processing your request.  Please refresh this page to try again and if you continue to see this message, contact <span style='font-weight:600;'>Nooch Support</span>:" +
                            "<br/><a href='mailto:support@nooch.com' style='display:block;margin:12px auto;font-weight:600;' target='_blank'>support@nooch.com</a>";
        }

        if (shouldShowErrorDiv == true)
        {
            $(".navbar").removeClass('hidden');
            $("#idVerify").fadeOut();
            $(".errorMessage").removeClass('hidden');
            $(".errorMessage").html(alertBodyText);
            $(".errorMessage a").addClass("btn btn-default m-t-20 animated bounceIn");
        }
        else
        {
            $(".navbar").addClass('hidden');
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
            if (isConfirm)
            {
                displayLoadingBox("Working...");
                setTimeout(function () {
                    window.top.location.href = RED_URL;
                }, 500);
            }
            else
            {
                window.open("mailto:support@nooch.com");
            }
        });
    }
}
