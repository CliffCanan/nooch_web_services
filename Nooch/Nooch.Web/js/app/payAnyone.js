var resultReason = "";
var MEMBER_TAG = "";

function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);
    return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}

$(document).ready(function () {
    MEMBER_TAG = getParameterByName('pay');
    console.log('Member Tag is: ' + MEMBER_TAG);

    hideform();
    ResetPage();

	// Submit User Profile Info Form
	$('form#PayorInitialInfoForm').submit(function(e) {
	    e.preventDefault();

		checkFields123();
    });

	// Submit User PW Form
	$('form#crtPwFrm').submit(function(e) {
	    e.preventDefault();

		checkPwForm();
    });
});


function checkFields123() {
    var id = "";

	// First validate the 'User Name' input
    var fullName = $('#userName').val().trim();
    $('#userName').val(fullName);
    var nameLngth = $('#userName').val().length;
    var spaceIndx = $('#userName').val().indexOf(" ");

	if ( $('#userName').parsley().validate() === true &&
	    (nameLngth > 4 && spaceIndx > 1  && spaceIndx < (nameLngth - 1))) // Make sure there are at least 2 names
	{
	    $('#usrNameGrp .errorMsg').removeClass('filled');

	    // If User Name is ok, then validate 'Email'
	    var emailTrimmed = $('#userEmail').val().trim();
	    $('#userEmail').val(emailTrimmed);

	    if ($('#userEmail').parsley().validate() === true)
	    {
	        // If Email is ok, then validate 'Phone'
	        var $strippedNum = $('#userPhone').val().trim();
	        $strippedNum = $strippedNum.replace(/\D/g, "");

	        $('#userPhoneHddn').val($strippedNum);

			if ($('#userPhoneHddn').parsley().validate() === true)
			{
				$('form#PayorInitialInfoForm').velocity("transition.slideLeftBigOut",function(){
					$('form#crtPwFrm').velocity("transition.slideRightBigIn",function() {
					    $('#userPassword').focus();
					});
				});
			}
			else {
			    id = "#usrPhoneGrp";
			}
		}
		else 
		{
			id = "#usrEmailGrp";
		}
	}
	else 
	{
		console.log("1st else statement reached - USERS NAME");
		if ( $('#userName').parsley().validate() === true)
		{
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
	// If password is ok 
	console.log("***** userPassword value is: " + $('#userPassword').val() + " *******");
    if ($('#userPassword').val().length == 0 ||
		($('#userPassword').val().length > 5 &&
		 $('#userPassword').parsley().validate() != false))
    {
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
	else
	{
		$('#userPassword').removeClass('parsley-success').addClass('parsley-error');
		$('#usrPwGrp .errorMsg').text('Please enter a slightly longer password :-)').addClass('parsley-errors-list').addClass('filled');
		shakeInputField("#usrPwGrp");
	}
}


// When Parsley validation fails, this function custom formats the field
function shakeInputField(inputGrpId) {
	$(inputGrpId + ' input').velocity("callout.shake");
	$(inputGrpId + ' .fa').css('color','#cf1a17')
                                .velocity('callout.shake')
                                .velocity({'color':'#3fabe1'},{delay:800});
	$(inputGrpId + ' input').focus();
}


function createRecord() {
    console.log('createRecord got called');

    $.ajax({
        type: "POST",
        url: "RegisterUserWithSynp",
        data: "{ transId: '" + $('#hidfield').val() + "',userEmail: '" + $('#userEmail').val() + "',userPhone: '" + $('#userPhone').val() + "',userName: '" + $('#userName').val() + "',userPassword: '" + $('#userPassword').val() + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
			$('#body-depositNew').unblock();

            var RegisterUserWithSynpResult = msg;
            console.log("SUCCESS --> 'RegisterUserWithSynpResult' is... ");
            console.log(RegisterUserWithSynpResult);
			resultReason = RegisterUserWithSynpResult.reason;

			if (RegisterUserWithSynpResult.success == "true" &&
			    RegisterUserWithSynpResult.memberIdGenerated.length > 5)
			{
                $('#PayorInitialInfo').addClass('hidden');
                $('#AddBankDiv').removeClass('hidden');

                $("#frame").attr("src", "http://localhost:4199/trans/Add-Bank.aspx?MemberId=" + RegisterUserWithSynpResult.memberIdGenerated + "&redUrl=http://localhost:4199/trans/payRequestComplete.aspx?mem_id=" + RegisterUserWithSynpResult.memberIdGenerated + "," + $('#hidfield').val());
                //$("#frame").attr("src", "https://www.noochme.com/noochweb/trans/Add-Bank.aspx?MemberId=" + RegisterUserWithSynpResult.memberIdGenerated + "&redUrl=https://www.noochme.com/noochweb/trans/payRequestComplete-syn.aspx?mem_id=" + RegisterUserWithSynpResult.memberIdGenerated + "," + $('#hidfield').val());
            }
			else 
			{
				if (resultReason.indexOf("already reg") >= 0)
				{
					showErrorModal('1');
					backAstep();
				}
				else
				{
					showErrorModal('404');
				}
            }
        },
        Error: function (x, e) { // On Error
            // Hide UIBlock (loading box)) 
			$('#body-depositNew').unblock();
			console.log("ERROR --> 'x' is: " + x + "  'e' is: " + e);

			showErrorModal('404');
        }
    });
}


function backAstep() {
	$('form#crtPwFrm').velocity("transition.slideRightBigOut",function(){
		$('form#PayorInitialInfoForm').velocity("transition.slideLeftBigIn");
	});
}


function showErrorModal(errorNum)
{
	var modalBodyTitle = "";
	var modalBodyText = "";

	console.log('2. resultReason is: ' + resultReason + ". And errorNum is: " + errorNum);

	if (errorNum == '1')
	{
		$('#reloadBtn_modal').addClass('hidden');

		modalBodyTitle = "Email Already Registered";
		modalBodyText = "Looks like that email address is already registered to a Nooch account.  Please try a different email address.";

		$("#okBtn").attr('onclick', 'dissmissMod_toEmail()');
		$('#userEmail').removeClass('parsley-success');
		$('#userEmail').addClass('parsley-error');
	}
	else if (errorNum == '2')
	{
	    $('#reloadBtn_modal').removeClass('hidden');

	    modalBodyTitle = "Unknown Nooch Tag";
	    modalBodyText = "Very sorry about this, but we're having trouble finding a Nooch member with that tag!  Please try again, or if this message persists, contact support@nooch.com for additional help.";
	}
	else if (errorNum == '404')
	{
		$('#reloadBtn_modal').removeClass('hidden');

		modalBodyTitle = "Errors are annoying";
		modalBodyText = "Very sorry about this, but we're having trouble processing your information, but the exact reason is not clear.  Please try again, or if this message persists, contact support@nooch.com for additional help.";
	}
	else
	{
		$('#reloadBtn_modal').removeClass('hidden');

		modalBodyTitle = "Errors are annoying";
		modalBodyText = "Very sorry about this, but we're having trouble processing your information, but the exact reason is not clear.  Please try again, or if this message persists, contact support@nooch.com for additional help.";
	}


	$('#modal_error #titleTxt').text(modalBodyTitle);
	$('#modal_error .modal-body > p').text(modalBodyText);
	$('#modal_error').modal({
		backdrop: 'static'
	});
}


function reloadBtnClckd() {
	$('#modal_error').modal('hide');
	setTimeout(location.reload(),500);
}


function dissmissMod_toEmail() {
	shakeInputField("#usrEmailGrp");
}


function ResetPage() {
    var memberTag = getParameterByName('pay');

    $('#PayorInitialInfo').show();
}


function hideform() 
{
    $("#PayorInitialInfo").removeClass('hidden');
}