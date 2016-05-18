var Is_PIN_REQUIRED = false;
var MFA_TYPE = '';
var MEMBER_ID = '';
var RED_URL = '';
var BANK_NAME = '';
var SEC_QUES_NO = 0;
var fromLandlordApp = '';
var sendToIdVerQuestions = false;
var isManual = false;

var step1height = '554px';
var step2height = '520px';

var wasEnterPressed = false;

$('#bankSearch').on('keypress', function (e) {
    // This function prevents the form from submitting... otherwise the browser was always selecting Bank of America no matter what was in the text field, not sure why
    if (e.which === 13) {
        wasEnterPressed = true;
    }
});

/**** (Step 1) USER SELECTS A BANK  ****/
$("#popularBanks .popBank-shell").click(function (e) {
    if (wasEnterPressed == false) {
        var bnkName = $(this).data("bname");
        bankHasBeenSelected(bnkName);
    }
	return false;
});

/**** (Step 2) USER JUST SELECTED A BANK ****/
function bankHasBeenSelected(selectedBankName) {
	// ADD THE LOADING BOX
	$('.addBankContainer-body').block({
		message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Connecting...</span>', 
		css: {
            border: 'none',
            padding: '26px 8px 20px',
            backgroundColor: '#000',
            '-webkit-border-radius': '15px',
            '-moz-border-radius': '15px',
            'border-radius': '15px',
            opacity: '.8',
			width: '70%',
			left: '15%',
			top: '35px',
            color: '#fff'
        }
	});

	$('#selectedBankInput').val(selectedBankName);

	CheckBankDetails(selectedBankName);
}


/**** FOR MANUAL BANK SUBMISSION W/ ROUTING & ACCOUNT NUMBER ****/
$(".manualLogin").click(function () {
	$('#bckbtn').removeClass("hide");

	$('.addBank-steps #step1').removeClass("active");
	$('.addBank-steps #step2').addClass("active");
	$('.addBank-steps #step2').addClass("filledIn");
	$('.addBank-steps #step2').text("2. Enter Info");

	$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/bank.png');
	resetBankLogoSize();

    $('.addBank-steps #step3').velocity({
		opacity:0
	},{
		duration:600,
		easing:'easeOutExpo',
		complete: function() {
			$('.addBank-steps #step3').addClass("hide");

			$('.addBank-steps #step2').velocity({
				'border-top-right-radius': '5px',
				'border-bottom-right-radius': '5px'
			},{
				duration:300
			})
		}
	});

	$('body.body').css("overflow","visible");
	var step2height_manual = '586px';

	if (fromLandlordApp == "yes")
	{
        step2height_manual = '672px'
	}
	if ($(window).width() > 767)
	{
	    step2height_manual = '650px'
	}
	//console.log("step2height_manual: " + step2height_manual);
	$('#addBankManual').removeClass("hide",function() {
		$('.addBank-container').velocity({
			height: step2height_manual
		},{
			duration: 1000,
			easing: "easeOutQuart"
		});

		$('#wideContainer').velocity({
			left: '-102%'
		},{
			duration: 1200,
			easing:[725,19],
			complete: function(e) {
				$('.anualLogin').velocity({opacity:0},{
					duration: 400,
					complete: function(e) {
						$('.manualLogin').addClass("hide");
					}
				});
				
				$('#userFullName').focus();
			}
		});

		$('#addBank1').velocity({opacity:0},{duration:500});
	});

	$('#bankLoginManual #userFullName').parsley().reset();
	$('#bankLoginManual #bankRout').parsley().reset();
    $('#bankLoginManual #bankAcntNum').parsley().reset();
	//$('#bankLoginManual #bankRout').val('');
    $('#bankLoginManual #bankAcntNum').val('');
});


// HANDLING BACK BUTTONS
$("#bckbtn").click(function () {
	goBackReset();
});
$("#backbtn2").click(function () {
	goBackReset();
});
$("#backbtn_onManual").click(function () {
	$('.addBank-steps #step2').velocity({
		'border-top-right-radius': '0px',
		'border-bottom-right-radius': '0px'
	},{
		duration:300
	});
	goBackReset();
});
function goBackReset() {
	$('#bckbtn').addClass("hide");
	$('.manualLogin').css("opacity", "1");

	$( "#bankSearch" ).val('');

	$('.addBank-container').velocity({height:step1height},{duration:600},{easing:'easeOutQuart'});

	$('#addBank1').velocity({opacity:1},{duration:700},{easing:'easeOutExpo'});

	$('#wideContainer').velocity({
		left: 0,
		height:'460px'
	},{
		duration:900,
		easing:"easeInOutQuart",
		delay: 200,
		complete: function(e) {
			if (!$('#bnkPinGrp').hasClass("hide")) {
				$('#bnkPinGrp').addClass("hide");
			}
			$('#addBank2').addClass("hide");
			$('#addBank_mfa_code').addClass("hide");
			$('#addBank_mfa_question').addClass("hide");
			$('#addBank_selectAccount').addClass("hide");
			$('#addBankManual').addClass("hide");

			if ($('.manualLogin').hasClass("hide")) {
				$('.manualLogin').removeClass("hide");
			}

			// Reset Login Error Message div if an error message is displayed
			if ($('#bankLogin_errorMsg p').length) {
				$('#bankLogin_errorMsg').html('');
			}
			if ($('#mfa_question_errorMsg p').length) {
				$('#mfa_question_errorMsg').html('');
			}
		}
	});


	$('.addBank-steps #step1').addClass("active");
	$('.addBank-steps #step2').removeClass("active");
	$('.addBank-steps #step2').removeClass("filledIn");
	$('.addBank-steps #step2').addClass("no-cursor");
	$('.addBank-steps #step2').attr("data-target","#modal-selectBankWarning");

	if ($('.addBank-steps #step3').hasClass("hide"))
	{
		$('.addBank-steps #step3').removeClass("hide");
		$('.addBank-steps #step3').velocity({
			opacity:1
		},{
			duration:500
		});

		$('.addBank-steps #step3').removeClass("hide");
		$('.addBank-steps #step2').text("2. Sign In");
	}
	wasEnterPressed = false;
}

/**** (Step 3) User still just selected a bank, getting general bank details from server ****/
function CheckBankDetails(bankName) {
	//console.log("CheckBankDetails - Bank: [" + bankName + "]");

    BANK_NAME = bankName;
    $.ajax({
        type: "POST",
        url: "CheckBankDetails",
        data: "{ bankname: '" + bankName + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
            // On Success                 
            console.log(msg);

			// Hide UIBlock (loading box))
            $('.addBankContainer-body').unblock();

            if (msg.IsSuccess == true) 
			{
                if (msg.IsPinRequired == false)
                {
                    Is_PIN_REQUIRED = false;
                    $('#bankPin').attr('data-parsley-required', 'false');
                }
                else
                {
                    // PIN should only be required for USAA bank (as of 7/29/15)
                    Is_PIN_REQUIRED = true;
                    $('#bnkPinGrp').removeClass("hide");
                    $('#bankPin').attr('data-parsley-required', 'true');
                    $('#bankLogin #bankPin').parsley().reset();

                    if ($(window).width() < 1000)
                    {
                        step2height = '580px';
                    }
                    else
                    {
                        step2height = '590px';
                    }
                }
                $('#bankLogin #bankUsername').parsley().reset();
                $('#bankLogin #bankPassword').parsley().reset();
            }
            else
            {
                // Bank was not found
				$('#modal-bankNotFound').modal({
					backdrop:'static'
				});
				return;
			}

            $('#addBank2').removeClass("hide", function ()
            {
				$('.addBank-container').velocity({
					height: step2height
				},{
					duration:1000,
					easing: "easeOutQuart"
				});

				$('#wideContainer').velocity({
					left: '-102%'
				},{
					duration:1250,
					easing:[725,19],
					complete: function (e) {
						$('#bankUsername').focus();
					}
				});

				$('.manualLogin').velocity({ opacity: "0" }, {
				    duration: 500,
				    complete: function (e) {
				        $('#manualLogin').addClass("hide");
				    }
				});
				$('#addBank1').velocity({opacity:0},{duration:500});
			});

            $('#bckbtn').toggleClass("hide");
            $('.addBank-steps #step1').removeClass("active",200,"easeInOutQuint");
            $('.addBank-steps #step2').addClass("filledIn",200,"easeInOutQuint");
            $('.addBank-steps #step2').addClass("active"),200,"easeInOutQuint";
            $('.addBank-steps #step2').addClass("no-cursor");
            $('.addBank-steps #step2').removeAttr("data-target");

            if (bankName == "Bank of America") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/bankofamerica.png');
            }
            else if (bankName == "Wells Fargo") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/WellsFargo.png');
            }
            else if (bankName == "Chase") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/chase.png');
            }
            else if (bankName == "Citibank") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/citibank.png');
            }
            else if (bankName == "TD Bank") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/td.png');
            }
            else if (bankName == "Capital One 360") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/capone360.png');
            }
            else if (bankName == "US Bank") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/usbank.png');
            }
            else if (bankName == "PNC") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/pnc.png');
            }
            else if (bankName == "SunTrust") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/suntrust.png');
            }
            else if (bankName == "USAA") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/usaa.png');
            }
            else if (bankName == "Ally") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/ally.png');
            }
            else if (bankName == "First Tennessee") {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/firsttennessee.png');
            }
            else if (bankName.indexOf("Regions") >= 0){
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/regions.png');
            }
            else {
                $(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/bank.png');
            }

			//Reset bank username/password fields (if person started to enter, then goes back to select a different bank)
			//$('#bankUsername').val('');
			$('#bankPassword').val('');
        },
        Error: function (x, e) {
            // On Error

			// Hide UIBlock (loading box))
			$('.addBankContainer-body').unblock();

			$('#modal-bankNotFound').modal({
				backdrop:'static'
			});
        }
    });
}


// Submit Online Banking Login username/password
$('#bankLogin').submit(function(e) {
	e.preventDefault();

	// Reset Login Error Message div IF an error message is displayed
	if ($('#bankLogin_errorMsg p').length) {
		$('#bankLogin_errorMsg div').slideUp(500,function() {
			$('#bankLogin_errorMsg').html('');
		});
	}

	// First validate the 'Username' input
	if ($('#bankLogin #bankUsername').parsley().validate() === true)
	{
		//If username is ok, then validate 'Password'
		if ($('#bankLogin #bankPassword').parsley().validate() === true)
		{
			// If password is ok AND PIN is required (for USAA bank), then validate 'Pin' field
			// OR if PIN is NOT required
		    if (!Is_PIN_REQUIRED || (Is_PIN_REQUIRED && $('#bankLogin #bankPin').parsley().validate() === true))
			{
				// ADD THE LOADING BOX
		        $('.addBankContainer-body').block({
				    message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Attempting login...</span>',
				    css: {
				        border: 'none',
				        padding: '26px 8px 20px',
				        backgroundColor: '#000',
				        '-webkit-border-radius': '15px',
				        '-moz-border-radius': '15px',
						'border-radius': '15px',
				        opacity: '.8',
				        width: '70%',
				        left: '15%',
				        top: '35px',
				        color: '#fff' 
				    }
				});

				submitBnkLgn();
			}
			else if (Is_PIN_REQUIRED) 
			{
				$('#bnkPinGrp input').velocity("callout.shake");
				$('#bnkPinGrp .fa').css('color','#cf1a17')
			                      .velocity('callout.shake')
								  .velocity({'color':'#3fabe1'},{delay:800});
				$('#bankPin').focus();
			}
		}
		else 
		{
			$('#bnkPwNameGrp input').velocity("callout.shake");
			$('#bnkPwNameGrp .fa').css('color','#cf1a17')
			                      .velocity('callout.shake')
								  .velocity({'color':'#3fabe1'},{delay:800});
			$('#bankPassword').focus();
		}
	}
	else 
	{
		$('#bnkUsrNameGrp input').velocity("callout.shake");
		$('#bnkUsrNameGrp .fa').css('color','#cf1a17')
		                       .velocity('callout.shake')
							   .velocity({'color':'#3fabe1'},{delay:800});
		$('#bankUsername').focus();
	}

});


function submitBnkLgn() {
    isManual = false;
    
    //console.log("submitBnkLgn Fired.");

    $.ajax({
        type: "POST",
        url: "BankLogin",
        data: "{ username: '" + $('#bankUsername').val() + "',password: '" + $('#bankPassword').val() + "',memberid: '" + MEMBER_ID + "',bankname: '" + BANK_NAME + "',IsPinRequired: '" + Is_PIN_REQUIRED + "',PinNumber: '" + $('#bankPin').val() + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
            
            console.log(msg);

			// Hide UIBlock (loading box))
            $('.addBankContainer-body').unblock();

			$('#bankUsername').parsley().reset();
			$('#bankPassword').parsley().reset();
			// Reset the PW field no matter what error it is
			$('#bankPassword').val('');

            var bnkLoginResult = msg;

			if (bnkLoginResult.Is_success == false) // ERROR CAME BACK FROM SERVER LOGIN ATTEMPT
			{
				console.log("submit bnk login error is: " + bnkLoginResult.ERROR_MSG);

				$('#bankLogin').velocity("callout.shake");

				if (bnkLoginResult.ERROR_MSG.indexOf('username provided was not correct') >= 0 ||
					bnkLoginResult.ERROR_MSG.indexOf('Please Enter the Correct Username and Password') >= 0 ||
					bnkLoginResult.ERROR_MSG.indexOf('username or password provided were not correct') >= 0)
				{
					$('#bankLogin_errorMsg').append("<div><p class='parsley-errors-list filled'>Invalid Credentials: Incorrect username or password.</p></div>");
					$('#bankUsername').focus();
				}
				else if (bnkLoginResult.ERROR_MSG.indexOf('password provided was not correct') >= 0)
				{
					$('#bankLogin_errorMsg').append("<div><p class='parsley-errors-list filled'>The password provided was not correct.</p></div>");
					$('#bankUsername').focus();
				}
				else if (bnkLoginResult.ERROR_MSG.indexOf('user phone not verified') >= 0)
				{
				    $('#bankLogin_errorMsg').append("<div><p class='parsley-errors-list filled'>For security, your phone number must be verified before you can link your bank. Go to your profile to verify.</p></div>");
				    $('#bankUsername').val('');
				    $('#bankUsername').focus();
				}
				else if (bnkLoginResult.ERROR_MSG.indexOf('Currently we are unable to login to') >= 0 ||
				         bnkLoginResult.ERROR_MSG.indexOf('Please try again later') >= 0)
				{
				    // ADD PROMPT FOR MANUAL ROUNTING/ACCOUNT #
				    bankLoginErrorAlert();

					$('#bankLogin_errorMsg').append("<div><p class='parsley-errors-list filled'>Oh no! Looks like we are experiencing some temporary trouble with " + BANK_NAME + ". :-( Please try again or contact Nooch Support.</p></div>");
				}
				else if (bnkLoginResult.ERROR_MSG.indexOf("account has not been fully set up") >= 0 ||
				         bnkLoginResult.ERROR_MSG.indexOf("Prompt the user to visit the issuing institution's site and finish the setup process") >= 0)
				{
					$('#modal-OtherError #par1').html("Looks like that " + BANK_NAME + " account is not fully set up for online banking yet.  Please visit " + BANK_NAME + "'s website to complete the setup process.");
					$('#modal-OtherError #par2').html("After setting up your online banking, then try connecting it to Nooch again.  If you continue to see this error we'd appreciate hearing about it at <a href='mailto:support@nooch.com' style='font-weight: 500;'>support@nooch.com</a>.");
					$('#modal-OtherError').modal({
						backdrop:'static'
					});
				}
				else if (bnkLoginResult.ERROR_MSG.indexOf('Missing ') >= -1)
				{
					$('#bankLogin_errorMsg').append("<div><p class='parsley-errors-list filled'>" + bnkLoginResult.ERROR_MSG + "</p></div>");

					swal({
						title: "Error #511: Incomplete Profile",
						text: "Very sorry about this, but it looks like your account is missing some information needed to verify your ID.<span class='show' style='margin-top:14px'>This can happen for a variety of reasons - our #1 priority is keeping your money safe, so sometimes our system is overly conservative.<span class='show' style='margin-top:14px;'><a href='mailto:support@nooch.com' target='_blank'>Contact our support team</a> and we'll be glad to help. Please mention:<br/><strong>Error #511</strong></span>",
						type: "error",
						showCancelButton: true,
						confirmButtonColor: "#3fabe1",
						confirmButtonText: "Ok",
						cancelButtonText: "Contact Support",
						closeOnCancel: false,
						html: true
					}, function (isConfirm) {
						if (!isConfirm) {
							window.open("mailto:support@nooch.com");
						}
					});
				}
				else if (bnkLoginResult.ERROR_MSG.indexOf('error occured at server') >= 0)
				{
					$('#bankLogin_errorMsg').append("<div><p class='parsley-errors-list filled'>Oh no! We experienced some trouble connecting to " + BANK_NAME + ". Please try again!</p></div>");
					$('#bankUsername').val('');
					$('#bankUsername').focus();
				}
				else
				{
					$('#bankLogin_errorMsg').append("<div><p class='parsley-errors-list filled'>Oh no! We are having some trouble connecting to " + BANK_NAME + ". Please try again!</p></div>");
					//$('#bankUsername').val('');
					//$('#bankUsername').focus();

				    // ADD PROMPT FOR MANUAL ROUNTING/ACCOUNT #
					bankLoginErrorAlert();
				}

				return;
			}

			// Hide the encryption-notice Div - not needed any more and takes up valuable space
			$('.encryption-notice').slideUp();

			$('#bckbtn').toggleClass("hide");


            // Check if Additional ID Verification Questions Are Needed
			//console.log("ssn_ver status was: [" + bnkLoginResult.ssn_verify_status + "]");

            if (bnkLoginResult.ssn_verify_status != null &&
                bnkLoginResult.ssn_verify_status.indexOf("additional") > -1)
			{
			    // Will need to send user to answer ID verification questions
			    console.log("submitBnkLgn -> Need to answer ID verification questions");

			    sendToIdVerQuestions = true;
			}


			// CHECK IF MFA IS REQUIRED
			if (bnkLoginResult.Is_MFA == true)
			{
			    MFA_TYPE = "question";

			    SEC_QUES_NO++;

			    // Show QUESTION based auth form
			    $('#addBank2').addClass("hide");
			    $('#addBank_mfa_question').removeClass("hide");
			    $('div.dividerLine').velocity({
			        width: '61.8%'
			    }, {
			        duration: 1000,
			        easing: 'easeInOutQuad'
			    });

			    $('#bankAccessTokenForQuestion').val(bnkLoginResult.bankoid);

			    $('#addBank-sec-question').parsley().reset();

			    // Displaying First MFA Question
			    $('#securityQuestionOneFromServer').html(bnkLoginResult.mfaMessage);
			}

			// NO MFA... ARRAY OF BANKS RETURNED
			else if (bnkLoginResult.Is_MFA == false && bnkLoginResult.SynapseBanksList != null)
			{
			    MFA_TYPE = "";

				// Go directly to final step: Show Select Account Div
				$('#addBank2').addClass("hide");
				$('#addBank_selectAccount').removeClass("hide");
				$('.addBank-steps #step2').removeClass("active");
				$('.addBank-steps #step3').addClass("active");
				$('.addBank-steps #step3').toggleClass("filledIn");

				var accountnum = 0;
				var bankht = "";

				$.each(bnkLoginResult.SynapseBanksList.banks, function (i, val) {
                    var currentBankAccnt = val;
                    accountnum += 1;
                    //console.log('Account nickname is: ' + currentBankAccnt.nickname);

                    // making html to add in div of banks selection list
                    bankht = bankht + "<div id='account" + accountnum + "Grp' class='input-group'><span class='input-group-addon'><input type='radio' name='account' id='account" + accountnum + "' value='" + currentBankAccnt.bankoid + "' data-bname='" + currentBankAccnt.bank_name + "' required data-parsley-required-message='Please select which account you would like to link to Nooch.' data-parsley-errors-container='#errorMsgDiv'></span><label class='form-control' id='label"+accountnum+"'><p class='form-control-static acntNm'>";
                    bankht = bankht + currentBankAccnt.nickname + "<span class='pull-right'>" + currentBankAccnt.account_number_string+"</span></p></label></div>";
                });

                $('#allAccounts').append(bankht);
				if (accountnum == 1) {
					$('#account1').attr('checked',true);
					$('#bankSelction').removeClass('btn-gray').addClass('btn-success');
					$('#label1').addClass('selected');
				}

				$("#addBank-selectAccount #allAccounts input").change(function ()
				{
					$('#bankSelction').removeClass('btn-gray').addClass('btn-success');

					var x = $(this).attr('id');
					x = x.substring(x.length - 1);

					$("#addBank-selectAccount .form-control.selected").removeClass('selected');
					$('#label' + x).addClass('selected');
				});
			}
        },
        Error: function (x, e) {
            // On Error
            console.log("submitBnkLgn -> Ajax ERROR :-(");

            // Hide UIBlock (loading box))
            $('.addBankContainer-body').unblock();

            // ADD PROMPT FOR MANUAL ROUNTING/ACCOUNT #
            bankLoginErrorAlert();
        }
    });
}

function bankLoginErrorAlert() {
    swal({
        title: "Oh No!",
        text: "We're having trouble verifying your login information - very sorry about this. Please contact <a href='mailto:support@nooch.com'>support@nooch</a> if the problem persists, or you can skip this step by entering your bank's routing/account # instead.",
        type: "error",
        showCancelButton: true,
        cancelButtonText: "Ok",
        confirmButtonColor: "#3fabe1",
        confirmButtonText: "Link Manually Instead",
        customClass: "stackedBtns",
        html: true,
    }, function (isConfirm) {
        if (isConfirm)
        {
            if (!$('#addBank2').hasClass("hide"))
            {
                $('#addBank2').addClass("hide");
            }
            $(".manualLogin").trigger("click");
        }
        else
        {
            $('#bankUsername').focus();
        }
    });
}

// Submit Manual Bank Info (Routing/Account Nos.)
$('#bankLoginManual').submit(function(e) {
	e.preventDefault();

	// First validate the 'Full Name' input
	$('#userFullName').val($.trim($('#userFullName').val()));

	if ($('#userFullName').parsley().validate() === true)
	{
		// If username is ok, then validate Routing Number input
		$('#bankRout').val($.trim($('#bankRout').val()));
		if ($('#bankRout').parsley().validate() === true)
		{
			// If routing number is ok, then validate Account Number input
			$('#bankAcntNum').val($.trim($('#bankAcntNum').val()));
			if ($('#bankAcntNum').parsley().validate() === true)
			{
				// ADD THE LOADING BOX
			    $('.addBankContainer-body').block({
				    message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Submitting...</span>',
				    css: {
				        border: 'none',
				        padding: '20px 8px 14px',
				        backgroundColor: '#000',
				        '-webkit-border-radius': '12px',
				        '-moz-border-radius': '12px',
						'border-radius': '12px',
				        opacity: '.75',
				        width: '80%',
				        left: '10%',
				        top: '25px',
				        color: '#fff' 
				    }
				});

				submitManualBank();
			}
			else
			{
				$('#acntGroup input').velocity("callout.shake");
				$('#acntGroup .fa').velocity("callout.shake");
				$('#bankAcntNum').focus();
			}
		}
		else 
		{
			$('#routGroup input').velocity("callout.shake");
			$('#routGroup .fa').velocity("callout.shake");
			$('#bankRout').focus();
		}
	}
	else 
	{
		$('#fullNameGroup input').velocity("callout.shake");
		$('#fullNameGroup .fa').velocity("callout.shake");
		$('#userFullName').focus();
	}

});


function submitManualBank() {
	console.log("{MemberID: '" + MEMBER_ID + "'Full Name: '" + $('#userFullName').val() + "', Routing #: '" + $('#bankRout').val() + "', Account #: '" + $('#bankAcntNum').val() + "', Type: Checking: '" + $('#togChecking').is(':checked') + "', Savings: '" + $('#togSavings').is(':checked') + "'}");
	isManual = true;

	var typeString, classString;
	if ($('#togChecking').is(':checked') && !$('#togSavings').is(':checked'))
	{
		typeString = "1"
	}
	else
	{
		typeString = "2"
	}

	if ($('#togPersonal').is(':checked') && !$('#togBusiness').is(':checked'))
	{
		classString = "1"
	}
	else
	{
		classString = "2"
	}

	$.ajax({
        type: "POST",
        url: "addBank",
        data: "{memberid: '" + MEMBER_ID + "', fullname: '" + $('#userFullName').val() + "',routing: '" + $('#bankRout').val() + "',account: '" + $('#bankAcntNum').val() + "',nickname: '',cl: '" + classString + "',type: '" + typeString + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
            //console.log("SUBMIT BANK MANUAL response msg is...");
            //console.log(msg.d);
	
			// Hide UIBlock (loading box))
            $('.addBankContainer-body').unblock();

			// Reset Login Error Message div IF an error message is displayed
			if ($('#bankManual_errorMsg > p').length) {
				$('#bankManual_errorMsg').html('');
			}

            var bnkManualResult = msg.d;

			if (bnkManualResult.Is_success == true)
			{
			    // Check if Additional ID Verification Questions Are Needed
			    if (bnkManualResult.ssn_verify_status != null &&
                    bnkManualResult.ssn_verify_status.indexOf("additional") > -1)
			    {
			        // Will need to send user to answer ID verification questions
			        console.log("Need to answer ID verification questions");

			        sendToIdVerQuestions = true;
			    }

				sendToRedUrl();
			}
			else // ERROR CAME BACK FROM SERVER LOGIN ATTEMPT
			{
				console.log("SUBMIT BANK MANUAL ERROR IS: " + bnkManualResult.ERROR_MSG);

				$('#bankLogin').velocity("callout.shake");

				var errorText = "Something went wrong - very sorry about this. We hate it when something breaks! Please try again or contact support@nooch if the problem happens again.";

				if (bnkManualResult.ERROR_MSG.indexOf('Currently we are unable to login to') >= 0 ||
				    bnkManualResult.ERROR_MSG.indexOf('Please try again later') >= 0)
				{
				    errorText: "Something went wrong - terrible sorry about this. We hate it when something breaks! Please try again or contact support@nooch if the problem happens again."
				}
				else if (bnkManualResult.ERROR_MSG.indexOf('error occured at server') >= 0)
				{
				    errorText: "Something went wrong - extremely sorry about this. We hate it when something breaks! Please try again or contact support@nooch if the problem happens again."
				}

				swal({
					title: "Oh No!",
					text: errorText,
					type: "error",
					confirmButtonColor: "#3fabe1",
					confirmButtonText: "Ok"
				});

				return;
			}
		},
        Error: function (x, e) {
			console.log("Add Bank ERROR. x:")
			console.log(x);
			console.log("e: " + e);
			// Hide UIBlock (loading box))
			$('.addBankContainer-body').unblock();

			swal({
				title: "Oh No!",
				text: "Something went wrong - very sorry about this. We hate it when something breaks! Please try again or contact <a href='mailto:support@nooch.com'>support@nooch.com</a> if the problem happens again.",
				type: "error",
				confirmButtonColor: "#3fabe1",
				confirmButtonText: "Ok",
                html: true
			});
        }
    });
}


// Submit the MFA Question form
$('#addBank-sec-question').submit(function(e) {
	e.preventDefault();

	console.log("#addBank-sec-question -> Submit Fired.");
    console.log("SEC_QUES_NO value is: " + SEC_QUES_NO);

    // Check the right input based on which # question is being answered
	if (SEC_QUES_NO == 1)
	{
		if ($('#securityQuest1').parsley().validate() === true)
		{
			MFALogin();
		}
		else 
		{
			$('#securityQuest1').velocity("callout.shake");
			$('#securityQuest1').focus();
		}
	}
	else if (SEC_QUES_NO == 2)
	{
		if ($('#securityQuest2').parsley().validate() === true)
		{
			MFALogin();
		}
		else 
		{
			$('#securityQuest2').velocity("callout.shake");
			$('#securityQuest2').focus();
		}
	}
	else if (SEC_QUES_NO == 3)
	{
		if ($('#securityQuest3').parsley().validate() === true)
		{
			MFALogin();
		}
		else 
		{
			$('#securityQuest3').velocity("callout.shake");
			$('#securityQuest3').focus();
		}
	}
});


function MFALogin() {
    var mfaResp = '';
    var accessCode = $('#bankAccessTokenForQuestion').val();;

	console.log("SEC_QUES_NO value is: " + SEC_QUES_NO);

    // Check the right input based on which # question is being answered
	if (SEC_QUES_NO == 1) {
	    mfaResp = $('#securityQuest1').val();
	}
	else if (SEC_QUES_NO == 2) {
	    mfaResp = $('#securityQuest2').val();
	}
	else if (SEC_QUES_NO == 3) {
	    mfaResp = $('#securityQuest3').val();
	}

	// ADD THE LOADING BOX
    $('.addBankContainer-body').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Checking that response</span>',
		css: { 
			border: 'none', 
			padding: '26px 8px 26px',
			backgroundColor: '#000', 
			'-webkit-border-radius': '15px',
			'-moz-border-radius': '15px',
			opacity: '.8',
			width: '70%',
			left: '15%',
			top: '15px',
			color: '#fff' 
		}
	});

	console.log("Submitting MFA response with data:  {bank: '" + BANK_NAME + "', memberid: '" + MEMBER_ID + "', bankname: '" + BANK_NAME + "', MFA: '" + mfaResp + "', ba: '" + accessCode + "'}");
    $.ajax({
        type: "POST",
        url: $('#MFALoginUrl').val(),
        data: "{ bank: '" + BANK_NAME + "',memberid: '" + MEMBER_ID + "',MFA: '" + mfaResp + "',ba: '" + accessCode + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
            var res = msg;
            console.log(res);

            // Hide UIBlock (loading box))
            $('.addBankContainer-body').unblock();

			if (res.Is_success == true)
			{
				console.log('MFALogin response was SUCCESSFUL');
				//console.log(res);

				// Checking if response contains another MFA
				if (res.Is_MFA == true && res.mfaMessage != null)
				{
					// Expecting Question No. 2 here (1st question would come after Bank Login)

					$('#addBank-sec-question .sec-question-input').val('');

					if (SEC_QUES_NO == 1) {
						console.log("We got to SEC_QUES_NO == 1");

						$('#ques1Div').velocity("transition.slideLeftBigOut",600, function() {
							$('#ques2Div').removeClass('hide');
							$('#ques2Div').velocity("transition.expandIn",700, function() {
								$('#securityQuest2').focus();
							});
						});
					    $('#ques3Div').addClass('hide');
						$('#securityQuestionTwoFromServer').html(res.mfaMessage);
						$('#securityQuest1').attr('data-parsley-required', 'false');
						$('#securityQuest2').attr('data-parsley-required', 'true');
					}
					else if (SEC_QUES_NO == 2) {
						console.log("We got to SEC_QUES_NO == 2");

						$('#ques2Div').velocity("transition.slideLeftBigOut",600, function() {
							$('#ques3Div').removeClass('hide');
							$('#ques3Div').velocity("transition.expandIn",700, function() {
								$('#securityQuest3').focus();
							});
						});

						$('#securityQuestionThreeFromServer').html(res.mfaMessage);
						$('#securityQuest2').attr('data-parsley-required', 'false');
						$('#securityQuest3').attr('data-parsley-required', 'true');
					}
					else
					{
						console.log("We got to SEC_QUES_NO == else... we got a problem!");
						//shouldn't ever reach here, but just in case, we'll go back to the 1st Question div to display any additional MFA questions
						
						$('#ques1Div').removeClass('hide');
						$('#ques1Div').velocity("transition.expandIn",600);
						$('#ques2Div').velocity("transition.slideLeftBigOut",400);
						$('#ques3Div').velocity("transition.slideLeftBigOut",400);

						$('#securityQuest1').html('Security Question'); 
						$('#securityQuestionOneFromServer').html(res.mfaMessage);

						$('#securityQuest1').attr('data-parsley-required', 'true');
						$('#securityQuest2').attr('data-parsley-required', 'false');
						$('#securityQuest3').attr('data-parsley-required', 'false');
					}

					SEC_QUES_NO++; // incremented it to write question mfa for 2nd round.
					$('#bankAccessTokenForQuestion').val(res.bankoid);
				}

                else if (res.Is_MFA == false && res.SynapseBanksList != null)
				{
                    var accountnum = 0;
                    var bankht = "";
                    console.log(res);

                    // Iterate through each bank
                    $.each(res.SynapseBanksList.banks, function (i, val)
                    {
                        var currentBankAccnt = val;
                        accountnum += 1;

                        // Making html to add in <div> of banks selection list
                        bankht = bankht + "<div id='account" + accountnum + "Grp' class='input-group'><span class='input-group-addon'><input type='radio' name='account' id='account" + accountnum + "' value='" + currentBankAccnt.bankoid + "' data-bname='" + currentBankAccnt.bank_name + "' required data-parsley-required-message='Please select which account you would like to link to Nooch.' data-parsley-errors-container='#errorMsgDiv'></span><label class='form-control' id='label" + accountnum + "'><p class='form-control-static acntNm'>";
                        bankht = bankht + currentBankAccnt.nickname + "<span class='pull-right'>" + currentBankAccnt.account_number_string+"</span></p></label></div>";
                    });

                    $('#allAccounts').append(bankht);

					if (accountnum == 1) {
						$('#account1').attr('checked',true);
						$('#bankSelction').removeClass('btn-gray').addClass('btn-success');
						$('#label1').addClass('selected');
					}

					$("#addBank-selectAccount #allAccounts input").change(function ()
					{
						$('#bankSelction').removeClass('btn-gray').addClass('btn-success');

						var x = $(this).attr('id');
						x = x.substring(x.length - 1);

						$("#addBank-selectAccount .form-control.selected").removeClass('selected');
						$('#label' + x).addClass('selected');
					});

					// Go to final step: 'Select Account'
					$('#addBank_mfa_question').addClass("hide");
					$('#addBank_mfa_code').addClass("hide");
					$('#addBank_selectAccount').removeClass("hide");
					$('.addBank-steps #step2').removeClass("active");
					$('.addBank-steps #step3').addClass("active");
					$('.addBank-steps #step3').addClass("filledIn");
                }
            }
			else
			{
				// ERROR CAME BACK FROM Synapse FOR MFA ATTEMPT
				console.log("SUBMIT BANK LOGIN ERROR IS: " + res.ERROR_MSG + "]");
				$('#mfa_question_errorMsg').html("<div><p class='parsley-errors-list filled'>" + res.ERROR_MSG + "</p></div>");

				return;
			}
        },
        Error: function (x, e) {
            // On Error
			console.log('MFASubmit AJAX ERROR: ' + x + ', ' + e);

            // Hide UIBlock (loading box) & display error message
			$('.addBankContainer-body').unblock();

			$('#mfa_question_errorMsg').html("<div><p class='parsley-errors-list filled'>Oh no! We encountered an error when we tried to verify your answer :-(</p></div>");

			mfaErrorAlert();
		}
    });
}

function mfaErrorAlert() {
    swal({
        title: "Oh No!",
        text: "We're having trouble verifying your answer - very sorry about this. Contact <a href='mailto:support@nooch.com'>support@nooch</a> if the problem persists, or you can skip this step by entering your bank's routing/account # instead.",
        type: "error",
        showCancelButton: true,
        cancelButtonText: "Ok",
        confirmButtonColor: "#3fabe1",
        confirmButtonText: "Link Manually Instead",
        customClass: "stackedBtns",
        html: true,
    }, function (isConfirm) {
        if (isConfirm) {
            if (!$('#addBank_mfa_question').hasClass("hide")) {
                $('#addBank2').addClass("hide");
            }
            if (!$('#addBank_mfa_code').hasClass("hide")) {
                $('#addBank_mfa_code').addClass("hide");
            }
            if (!$('#addBank2').hasClass("hide")) {
                $('#addBank2').addClass("hide");
            }
            $(".manualLogin").trigger("click");
        }
    });
}

function SetDefaultAct() {

    if ($("#addBank-selectAccount").parsley().validate() === true)
	{
		var bankId = $("input:radio[name='account']:checked").val();
		var bankName = $("input:radio[name='account']:checked").data("bname");

		// ADD THE LOADING BOX
		$('.addBankContainer-body').block({
		    message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Linking Account...</span>',
			css: { 
				border: 'none', 
				padding: '20px 8px 14px',
				backgroundColor: '#000', 
				'-webkit-border-radius': '12px',
				'-moz-border-radius': '12px',
				'border-radius': '12px',
				opacity: '.8',
				width: '70%',
				left: '15%',
				top: '15px',
				color: '#fff' 
			}
		});

		$.ajax({
			type: "POST",
			url: $('#setDefaultBankUrl').val(),
			data: "{ MemberId: '" + MEMBER_ID + "',BankName: '" + bankName + "',BankOId: '" + bankId + "'}",
			contentType: "application/json; charset=utf-8",
			dataType: "json",
			async: "true",
			cache: "false",
			success: function (msg) {
				$('.addBankContainer-body').unblock();

				var res = msg;

				// REDIRECT THE USER TO THE RIGHT PLACE
				if (res.Is_success == true)
				{
				    sendToRedUrl();
				}
				else
				{
				    swal({
						title: "Oh No!",
						text: "Something went wrong - very sorry about this. We hate it when things break! Please try again or contact <a href='mailto:support@nooch.com' target='_blank'>support@nooch</a> and we'll be glad to help." +
						      "<small class='show' style='margin-top:12px'>Error Reference: <strong>#SD-1</strong></small>",
						type: "error",
						confirmButtonColor: "#3fabe1",
						confirmButtonText: "Ok",
						html: true
					});
				}
			},
			Error: function (x, e) {
			    $('.addBankContainer-body').unblock();
			}
		});
	}
	else 
	{
		console.log("PARSLEY RETURNED false FOR SELECT ACCOUNT");
		return;
	}
}


function sendToRedUrl() {
    console.log("RED_URL is: [" + RED_URL + "], sendToIdVerQuestions: [" + sendToIdVerQuestions + "]");

    // First, check if this is a Landlord
    if (fromLandlordApp == "yes")
    {
        //console.log("sendToRedUrl reached - fromLandlordApp = true");

        // Is Extra ID ver needed?
        if (sendToIdVerQuestions == true)
        {
            // Send msg back to parent window to display new iFrame with ID Ver URL
            console.log("TRIGGERING COMPLETE IN PARENT - extra verification needed!");
            window.parent.$('body').trigger('addBankComplete');
        }
        else
        {
            // Send msg back to parent window to display Success message for adding a bank
            console.log("TRIGGERING COMPLETE IN PARENT - NO extra ver needed - success!");
            window.parent.$('body').trigger('addBankComplete');
        }
    }
    else
    {
        // Non-Landlord...

        if (sendToIdVerQuestions == true)
        {
            console.log("sending to ID Verification page...");
            window.top.location.href = "https://www.noochme.com/noochweb/Nooch/idVerification?memid=" + MEMBER_ID + "&from=addbnk&redUrl=" + RED_URL;
        }
        else if (RED_URL.indexOf("rentscene") > -1) // For RentScene
        {
            if (isManual == true)
            {
                // Reset routing/account # form
                $('#userFullName').val('');
                $('#bankRout').val('');
                $('#bankAcntNum').val('');
            }

            swal({
                title: "Bank linked successfully!",
                text: "<p>Thanks for completing this <strong>one-time</strong> process. &nbsp;Now you can make payments without sharing your bank details.</p>" +
                      "<p>We will notify your landlord that you're ready to pay and we'll be in touch soon about completing your rent payments.</p>",
                type: "success",
                confirmButtonColor: "#3fabe1",
                confirmButtonText: "Done",
                customClass: "largeText",
                html: true
            }, function (isConfirm) {
                $('.addBankContainer-body').block({
                    message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Finishing...</span>',
                    css: {
                        border: 'none',
                        padding: '20px 8px 14px',
                        backgroundColor: '#000',
                        '-webkit-border-radius': '12px',
                        '-moz-border-radius': '12px',
                        'border-radius': '12px',
                        opacity: '.75',
                        width: '80%',
                        left: '10%',
                        top: '25px',
                        color: '#fff'
                    }
                });

                setTimeout(function () {
                    window.top.location.href = "https://www.nooch.com/nooch-for-landlords";
                }, 300);
            });
        }
        else if (RED_URL == "createaccnt")// For users coming from the CreateAccount.aspx page
        {
            // Send msg back to parent window to display Success message for adding a bank
            console.log("AddBank -> TRIGGERING COMPLETE IN PARENT - Success!");
            window.parent.$('body').trigger('addBankComplete');
        }
         
        else if (RED_URL.indexOf('DepositMoneyComplete') > 1 ||
                 RED_URL.indexOf('PayRequestComplete') > 1)
        {
            swal({
                title: "Bank Linked Successfully",
                text: "<p>Thanks for completing this <strong>one-time</strong> process. &nbsp;Now you can make secure payments with anyone and never share your bank details!</p>",
                type: "success",
                confirmButtonColor: "#3fabe1",
                confirmButtonText: "Done",
                customClass: "largeText",
                html: true
            }, function (isConfirm)
            {
                $('.addBankContainer-body').block({
                    message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Finishing...</span>',
                    css: {
                        border: 'none',
                        padding: '20px 8px 14px',
                        backgroundColor: '#000',
                        '-webkit-border-radius': '12px',
                        '-moz-border-radius': '12px',
                        'border-radius': '12px',
                        opacity: '.8',
                        width: '70%',
                        left: '15%',
                        top: '25px',
                        color: '#fff'
                    }
                });

                setTimeout(function () {
                    window.top.location.href = RED_URL;
                }, 300);
            });
        }
        else // All Others - most likely no RED_URL was passed in URL, so defaulting to a Sweet Alert
            {
                swal({
                    title: "Bank Linked Successfully",
                    text: "<p>Thanks for completing this <strong>one-time</strong> process. &nbsp;Now you can make secure payments with anyone and never share your bank details!</p>",
                    type: "success",
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Done",
                    customClass: "largeText",
                    html: true
                }, function (isConfirm) {
                    $('.addBankContainer-body').block({
                        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Finishing...</span>',
                        css: {
                            border: 'none',
                            padding: '26px 8px 20px',
                            backgroundColor: '#000',
                            '-webkit-border-radius': '15px',
                            '-moz-border-radius': '15px',
                            'border-radius': '15px',
                            opacity: '.8',
                            width: '70%',
                            left: '15%',
                            top: '25px',
                            color: '#fff'
                        }
                    });

                    setTimeout(function ()
                    {
                        window.top.location.href = "https://www.nooch.com/";
                    }, 300);
                });
            }
        }
}



$(document).ready(function () {
	$('.popBank-shell.group1').addClass('zoomIn').queue(function(){
		$(this).addClass('finishedAnimating').dequeue();
	});
	$('.popBank-shell.group2').delay(250).queue(function(){
		$(this).addClass('zoomIn').queue(function(){
			$(this).addClass('finishedAnimating').dequeue();
		}).dequeue();
	});
	$('.popBank-shell.group3').delay(400).queue(function(){
		$(this).addClass('zoomIn').queue(function(){
			$(this).addClass('finishedAnimating').dequeue();
		}).dequeue();
	});
	$('.popBank-shell.group4').delay(500).queue(function(){
		$(this).addClass('zoomIn').queue(function(){
			$(this).addClass('finishedAnimating').dequeue();
		}).dequeue();
	});


    function getParameterByName(name) {
        name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
        var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
            results = regex.exec(location.search);
        return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
    }

    MEMBER_ID = getParameterByName('MemberId');
    if (MEMBER_ID == null || MEMBER_ID.length < 1)
    {
        MEMBER_ID = getParameterByName('memberid');
    }

    RED_URL = getParameterByName('redUrl');

    $('#red_url').val(RED_URL);

    // Check if RED_URL is empty (wasn't passed in URL) or a variation of 'nooch.com'
    // The redirection will hit a 404 unless the entire URL is sent, including the protocol ('https')
    if (RED_URL.length < 2 || RED_URL.indexOf('nooch.com') > -1)
    {
        console.log("No RED_URL found!")
        RED_URL = "https://www.nooch.com"
    }

    fromLandlordApp = getParameterByName('ll');

	//console.log("fromLandlordApp is: [" + fromLandlordApp + "]");

	$('#bankNotListed').removeClass('hide');

	if (RED_URL.indexOf("nooch://") > -1)
	{
		// Coming from inside the app
		$('.addBank-heading').hide();
		$('.addBankContainer-body').css('padding-top', '0px');

		$('.encryption-notice').addClass('hide');
		
		step1height = '514px';
		step2height = '496px';
	}
	else if ((fromLandlordApp.indexOf("yes") > -1) || ($(window).width() > 500))
	{
	    $('html').addClass('landlord');

	    step1height = '652px';

	    //$('.addBank-heading').hide();
	}

	if ($(window).width() > 1100)
	{
	    step1height = '650px';
	    step2height = '540px';
	}

	$('.addBank-container').css("height", step1height);

	if (MEMBER_ID == null || MEMBER_ID.length < 30)
	{
	    swal({
	        title: "Configuration Error",
	        text: "It looks like we had trouble loading your Nooch account information.  Please contact <a href=\"mailto:support@nooch.com\">Nooch Support</a> to get help.",
	        type: "error",
	        confirmButtonColor: "#3fabe1",
	        confirmButtonText: "Ok",
            html: true
	    })
	}

	else if (RED_URL.indexOf("rentscene") > -1) {
	    $('#form1').append('<img src="https://noochme.com/noochweb/Assets/Images/rentscene.png" class="brandedLogo desk-only" />');

	    swal({
	        title: "Secure, Private Payments",
	        text: "<p>Rent Scene offers a quick, secure way to pay rent without giving your routing or account number. &nbsp;Just select your bank and login to your online banking<span class='desk-only'> as you normally do</span>.</p>" +
				  "<ul class='fa-ul'><li><i class='fa-li fa fa-check'></i><strong>We don't see or store</strong> your bank credentials</li>" +
				  "<li><i class='fa-li fa fa-check'></i>The person you pay never sees any of your personal or bank info (except your name)</li>" +
				  "<li><i class='fa-li fa fa-check'></i>All data is secured with <strong>bank-grade encryption</strong></li></ul>",
	        imageUrl: "../Assets/Images/secure.svg",
			imageSize: "194x80",
			showCancelButton: true,
			cancelButtonText: "Learn More",
	        confirmButtonColor: "#3fabe1",
	        confirmButtonText: "Great, Let's Go!",
	        customClass: "securityAlert",
	        allowEscapeKey: false,
	        html: true
	    }, function (isConfirm) {
			if (!isConfirm) {
				window.top.location.href = "https://www.nooch.com/safe";
			}
		});
	}

	/**** CREATING AUTO-COMPLETE LIST OF BANKS FOR SEARCHING ****/

	$("#bankSearch").autoComplete({
	    minChars: 2,
	    cache: true, // might want to turn off
	    delay: 0,
	    source: function(term, suggest) {
	        term = term.toLowerCase();
	        var choices = ["Ally Bank", "Bank of America", "Chase", "Citibank",
                           "Capital One 360", "Fidelity", "First Tennessee", "US Bank",
                           "USAA", "Wells Fargo", "PNC", "SunTrust", "TD Bank", "Regions Alabama",
                           "Regions Arkansas", "Regions Florida", "Regions Georgia", "Regions Illinois",
                           "Regions Indiana", "Regions Iowa", "Regions Kentucky", "Regions Louisiana",
                           "Regions Mississippi", "Regions Missouri", "Regions North Carolina",
                           "Regions South Carolina", "Regions Tennessee", "Regions Texas", "Regions Virginia"];
	        var matches = [];
	        for (i = 0; i < choices.length; i++)
	            if (~choices[i].toLowerCase().indexOf(term)) matches.push(choices[i]);
	        suggest(matches);
	    },
	    onSelect: function (e, term, item) {
	        //console.log("Term is: ");
	        //console.log(term);
	        //console.log("Item is: ");
	        //console.log(item);

	        var bnkName = term;
	        $("#bankSearch").val(bnkName);

	        $('#selectedBankInput').val(bnkName);

	        if (bnkName == "OTHER") {
	            $(".manualLogin").click();
	        }
	        else {
	            if (bnkName == "Ally Bank") {
	                bnkName = "Ally";
	            }
	            bankHasBeenSelected(bnkName);
	        }
	    }
	});

    console.log('Page loaded for MemberId: ' + MEMBER_ID);

    $("#bankSearch").on("input", function () {
        var numOfChars = $("#bankSearch").val().length;

        if (numOfChars > 2)
        {
            // Now check if there have been any bank matches yet...
            var numOfHits = $(".ui-autocomplete li").size()

            //console.log("There are [" + numOfHits + "] hits!");

            if (numOfHits < 4)
            {
                //console.log("There are less than 3 hits!!");

                if ($(".ui-autocomplete").css("display") == "none")
                {
                    setTimeout(function () {
                        $(".ui-autocomplete").css("display", "block").append("<li class=\"ui-menu-item ui-menu-item-other\" id=\"ui-id-other\" tab-index=\"-1\">My Bank Is Not Listed!</li>");
                    }, 400)
                }
                else
                {
                    $(".ui-autocomplete").append("<li class=\"ui-menu-item ui-menu-item-other\" id=\"ui-id-other\" tab-index=\"-1\">My Bank Is Not Listed!</li>");
                }
            }
        }
    });

	$("#bankRout").change(function()
	{
         var val = $(this).val().trim();
         val = val.replace(/\s+/g, '');

         if (val.length == 9) { //for checking 3 characters
               lookupRoutingNum(val);
         }
	});
});

function lookupRoutingNum(rn)
{
	$("#result").empty().html("<i class=\"fa fa-spinner fa-pulse\"></i>");
	$.ajax({
	    url: "https://routingnumbers.herokuapp.com/api/name.json?rn=" + rn,
		dataType: 'jsonp',
		success: onLookupSuccess
	});
}

function onLookupSuccess(data)
{
	//console.log(data);

	$("#result").empty();

	if (data["message"].toLowerCase() != "ok")
	{
		$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/bank.png');
	}
	else
	{
		var name = data["name"].toLowerCase();

		if (name.indexOf("bank of america") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/bankofamerica.png');
		}
		else if (name.indexOf("wells fargo") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/WellsFargo.png');
		}
		else if (name.indexOf("chase") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/chase.png');
		}
		else if (name.indexOf("citibank") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/citibank.png');
		}
		else if (name.indexOf("td bank") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/td.png');
		}
		else if (name.indexOf("capital one 360") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/capone360.png');
		}
		else if (name.indexOf("us bank") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/usbank.png');
		}
		else if (name.indexOf("pnc") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/pnc.png');
		}
		else if (name.indexOf("suntrust") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/suntrust.png');
		}
		else if (name.indexOf("usaa") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/usaa.png');
		}
		else if (name.indexOf("ally") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/ally.png');
		}
		else if (name.indexOf("first tennessee") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/firsttennessee.png');
		}
		else if (name.indexOf("regions") >= 0) {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/regions.png');
		}
		else {
			$(".selectedBank-logo img").attr('src', '../Assets/Images/bankPictures/bank.png');
		}
	}

	resetBankLogoSize();
}

function resetBankLogoSize() {
	var img = $(".selectedBank-logo img").attr('src');
	var width = "98px";

	if (img.indexOf("/bank.png") < 0) // if the image IS currently one of the actual bank logos
	{
		if ($(window).width() > 767)
		{
			width = "125px";
		}
		else
		{
			width = "110px";
		}
	}
	else // if the image is currently the default bank icon
	{
		if ($(window).width() > 767)
		{
			width = "98px";
		}
	}
	$("#addBankManual .selectedBank-logo > img").css({
		width: width,
		height: "auto"
	})
}
