# Email Delivery Troubleshooting Guide 📧

## Issue
OTP emails are not being received during signup.

## Root Cause Investigation

### ✅ Fixed Issues
1. **Transaction Error**: Fixed SqlTransaction rollback error by moving email sending after commit
2. **Silent Failure**: Email exceptions were being caught without user notification

### 🔍 Diagnostic Tools Added

#### 1. Enhanced Logging in SignUp
- Full exception details logged to console
- OTP printed to console for testing if email fails
- User warning message if email delivery fails

#### 2. Test Email Endpoint
- URL: `/Account/TestEmail`
- Purpose: Test SMTP configuration without signup
- Shows detailed error messages
- Displays current SMTP settings

### 📋 Common Email Issues with Gmail SMTP

#### Issue #1: App Password Not Used
**Symptom**: Authentication failure
**Solution**: 
1. Enable 2-Step Verification on Gmail account
2. Generate App Password at: https://myaccount.google.com/apppasswords
3. Use App Password (16 characters) in `appsettings.json`

#### Issue #2: Wrong Email Address
**Current Config**: `dasassignment7007@gmail.com`
**Verify**: Is this the correct email? (Not dsaassignment7007@gmail.com)

#### Issue #3: Less Secure Apps Disabled
**Symptom**: Access denied
**Solution**: Use App Password instead (preferred method)

#### Issue #4: SMTP Blocked by Firewall
**Symptom**: Connection timeout
**Solution**: 
- Check antivirus/firewall settings
- Allow port 587 outbound
- Try port 465 with SSL as alternative

#### Issue #5: Account Locked or Suspicious Activity
**Symptom**: Authentication fails
**Solution**:
- Check Gmail inbox for security alerts
- Verify account at: https://myaccount.google.com/
- Try sending test email from Gmail first

### 🧪 Testing Steps

#### Step 1: Test SMTP Configuration
```
1. Navigate to: http://localhost:5000/Account/TestEmail
2. Enter your email address
3. Click "Send Test Email"
4. Check console for errors
5. Check inbox (and spam folder)
```

#### Step 2: Check Console Output
When signup fails to send email, check the terminal for:
```
============ EMAIL SENDING FAILED ============
To: user@example.com
Error: [Error message here]
Stack Trace: [Stack trace]
Inner Exception: [Inner exception if any]
OTP (for testing): 123456
==============================================
```

#### Step 3: Manual OTP Entry (Temporary Solution)
If email fails:
1. Check console for OTP code
2. Copy the 6-digit OTP
3. Enter it on VerifyEmail page
4. Complete verification

### 🔧 Configuration Check

**Current Settings** (from appsettings.json):
```json
"EmailSettings": {
  "Email": "dasassignment7007@gmail.com",
  "AppPassword": "yqdaktgncnvpnrgg",
  "SmtpServer": "smtp.gmail.com",
  "Port": "587"
}
```

**Questions to Verify**:
- [ ] Is the email address correct?
- [ ] Is the App Password valid (not expired)?
- [ ] Has 2-Step Verification been enabled?
- [ ] Is the Gmail account active and not locked?

### 🚀 Quick Fix Options

#### Option 1: Use Console OTP (Development)
- Email sending errors are now caught and logged
- OTP is printed to console
- User can manually enter OTP from console

#### Option 2: Test Email Endpoint
- Go to `/Account/TestEmail`
- Send test email to verify SMTP works
- Debug any configuration issues

#### Option 3: Alternative Email Service
If Gmail continues to fail, consider:
- SendGrid (free tier: 100 emails/day)
- Mailgun (free tier: 5,000 emails/month)
- AWS SES (very low cost)

### 📝 Next Steps

1. **Run Application**: `dotnet run`
2. **Open Browser**: Navigate to `http://localhost:5000/Account/TestEmail`
3. **Test Email**: Enter your email and send test
4. **Check Results**: 
   - If success: Email configuration is working
   - If failure: Check console for detailed error
5. **Fix Configuration**: Based on error message
6. **Try Signup**: After email test passes

### 🔐 Gmail App Password Generation

1. Go to Google Account: https://myaccount.google.com/
2. Navigate to: Security → 2-Step Verification
3. Enable 2-Step Verification if not already enabled
4. Go back to Security
5. Click "App passwords"
6. Select app: "Mail"
7. Select device: "Windows Computer" or "Other"
8. Click "Generate"
9. Copy the 16-character password (format: xxxx xxxx xxxx xxxx)
10. Remove spaces and update `appsettings.json`:
   ```json
   "AppPassword": "xxxxxxxxxxxxxxxx"
   ```

### 🎯 Expected Behavior

**Successful Email Delivery**:
```
1. User fills signup form
2. Click "Sign Up"
3. Account created in database
4. OTP generated (e.g., 123456)
5. Email sent successfully
6. User redirected to VerifyEmail page
7. User receives email within 1-2 minutes
8. User enters OTP from email
9. Email verified, user logged in
10. Redirected to Admin Dashboard
```

**Failed Email Delivery (Now Handled Gracefully)**:
```
1. User fills signup form
2. Click "Sign Up"
3. Account created in database
4. OTP generated (e.g., 123456)
5. Email sending fails (SMTP error)
6. Error logged to console with OTP
7. User redirected to VerifyEmail page
8. Warning message shown: "Email delivery failed"
9. User checks console/terminal for OTP
10. User enters OTP from console
11. Email verified, user logged in
12. Redirected to Admin Dashboard
```

### 📊 Error Messages Reference

| Error Message | Cause | Solution |
|--------------|-------|----------|
| "Authentication failed" | Wrong App Password | Regenerate App Password |
| "Connection timeout" | Port blocked | Check firewall, try port 465 |
| "Mailbox unavailable" | Wrong email address | Verify email in config |
| "Access denied" | Less secure apps disabled | Use App Password |
| "Quota exceeded" | Too many emails sent | Wait or use different account |

### ✅ Verification Checklist

Before testing signup again:

- [ ] Run application: `dotnet run`
- [ ] Navigate to `/Account/TestEmail`
- [ ] Send test email to your personal email
- [ ] Verify email received successfully
- [ ] Check console shows "Test email sent successfully"
- [ ] If test passes, try signup with same email
- [ ] If test fails, read error message carefully
- [ ] Fix configuration based on error
- [ ] Retry test email until successful

---

## Current Status

✅ **Transaction error fixed**  
✅ **Logging enhanced**  
✅ **Test endpoint created**  
⏳ **Waiting for SMTP configuration verification**

**Next Action**: Test email delivery using `/Account/TestEmail` endpoint
