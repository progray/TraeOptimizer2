using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Optimizer.Services;

namespace Optimizer
{
    public sealed partial class LoginForm : Form
    {
        private readonly IAuthService _authService;
        private CancellationTokenSource _cts;
        private Task _loginTask;
        private int _progressValue = 0;
        private System.Windows.Forms.Timer _progressTimer;
        private int _totalLoginTimeMs = 0;
        private const int PROGRESS_UPDATE_INTERVAL_MS = 100;

        public DialogResult LoginResult { get; private set; } = DialogResult.Cancel;

        public LoginForm(IAuthService authService)
        {
            InitializeComponent();
            OptionsHelper.ApplyTheme(this);
            _authService = authService;
            InitializeProgressTimer();

            // Initialize UI state
            UpdateStatus("Ready to login");
            btnCancel.Enabled = false;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Maximum = 100;
            progressBar.Value = 0;

            // Translate UI elements
            TranslateUI();
        }

        private void InitializeProgressTimer()
        {
            _progressTimer = new System.Windows.Forms.Timer();
            _progressTimer.Interval = PROGRESS_UPDATE_INTERVAL_MS;
            _progressTimer.Tick += ProgressTimer_Tick;
        }

        private void TranslateUI()
        {
            if (OptionsHelper.CurrentOptions.LanguageCode == LanguageCode.EN) return;

            // Translate static UI elements
            btnLogin.Text = OptionsHelper.TranslationList.TryGetValue("LoginForm.btnLogin", out string loginText) ? loginText : "Login";
            btnCancel.Text = OptionsHelper.TranslationList.TryGetValue("LoginForm.btnCancel", out string cancelText) ? cancelText : "Cancel";
            this.Text = OptionsHelper.TranslationList.TryGetValue("LoginForm.Text", out string titleText) ? titleText : "Login";
        }

        private void UpdateStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action<string>(UpdateStatus), status);
                return;
            }
            lblStatus.Text = status;
        }

        private void UpdateProgress(int value)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action<int>(UpdateProgress), value);
                return;
            }
            progressBar.Value = value;
        }

        private void UpdateButtonStates(bool loginEnabled, bool cancelEnabled)
        {
            if (btnLogin.InvokeRequired || btnCancel.InvokeRequired)
            {
                Invoke(new Action<bool, bool>(UpdateButtonStates), loginEnabled, cancelEnabled);
                return;
            }
            btnLogin.Enabled = loginEnabled;
            btnCancel.Enabled = cancelEnabled;
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (_totalLoginTimeMs == 0) return;

            _progressValue += (int)((double)PROGRESS_UPDATE_INTERVAL_MS / _totalLoginTimeMs * 100);
            if (_progressValue > 100) _progressValue = 100;
            UpdateProgress(_progressValue);

            // Update status with remaining time
            int remainingSeconds = (int)(_totalLoginTimeMs - (_progressValue * _totalLoginTimeMs / 100)) / 1000;
            UpdateStatus($"Login in progress... (approx {remainingSeconds} seconds left)");
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                _cts = new CancellationTokenSource();
                _totalLoginTimeMs = new Random().Next(10000, 30000); // 10-30 seconds
                _progressValue = 0;

                UpdateButtonStates(false, true);
                UpdateStatus("Login in progress...");
                _progressTimer.Start();

                // Log the login attempt
                Logger.LogInfoSilent($"{nameof(LoginForm)}.{nameof(btnLogin_Click)}: Login started with {_totalLoginTimeMs / 1000}s delay");

                // Directly await the authentication method without Task.Run
                AuthResult result = await _authService.AuthenticateAsync(_cts.Token);
                
                if (result == AuthResult.Success)
                {
                    UpdateStatus("Login successful!");
                    Logger.LogInfoSilent($"{nameof(LoginForm)}.{nameof(btnLogin_Click)}: Login completed successfully");
                    LoginResult = DialogResult.OK;
                }
                else if (result == AuthResult.Cancelled)
                {
                    UpdateStatus("Login cancelled");
                    Logger.LogInfoSilent($"{nameof(LoginForm)}.{nameof(btnLogin_Click)}: Login cancelled by user");
                }
                else
                {
                    UpdateStatus("Login failed");
                    Logger.LogError($"{nameof(LoginForm)}.{nameof(btnLogin_Click)}", "Login failed", string.Empty);
                }
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Login cancelled");
                Logger.LogInfoSilent($"{nameof(LoginForm)}.{nameof(btnLogin_Click)}: Login cancelled");
            }
            catch (Exception ex)
            {
                UpdateStatus("Login failed: " + ex.Message);
                Logger.LogError($"{nameof(LoginForm)}.{nameof(btnLogin_Click)}", ex.Message, ex.StackTrace);
            }
            finally
            {
                _progressTimer.Stop();
                UpdateButtonStates(true, false);
                
                // Close the form after a short delay to show the final status
                await Task.Delay(1000);
                
                if (LoginResult == DialogResult.OK)
                {
                    OptionsHelper.CurrentOptions.IsLoggedIn = true;
                    OptionsHelper.SaveSettings();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                _cts?.Cancel();
                UpdateStatus("Cancelling login...");
                Logger.LogInfoSilent($"{nameof(LoginForm)}.{nameof(btnCancel_Click)}: Login cancellation requested");
            }
            catch (Exception ex)
            {
                Logger.LogError($"{nameof(LoginForm)}.{nameof(btnCancel_Click)}", ex.Message, ex.StackTrace);
            }
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            try
            {
                Logger.LogInfoSilent($"{nameof(LoginForm)}.{nameof(LoginForm_Load)}: Login form opened");
            }
            catch (Exception ex)
            {
                Logger.LogError($"{nameof(LoginForm)}.{nameof(LoginForm_Load)}", ex.Message, ex.StackTrace);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _cts?.Cancel();
                _progressTimer?.Stop();
                _progressTimer?.Dispose();
                
                if (LoginResult != DialogResult.OK)
                {
                    Logger.LogInfoSilent($"{nameof(LoginForm)}.{nameof(OnFormClosing)}: Login form closed without success");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"{nameof(LoginForm)}.{nameof(OnFormClosing)}", ex.Message, ex.StackTrace);
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }
    }
}