﻿using Microsoft.Win32;
using Presentation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PushshiftAPI
{
    public partial class MainForm : Form, ISearchOptions
    {

        #region Constants

        private static readonly DateTime START_DATE = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        #endregion

        #region Constructors

        public MainForm()
        {
            InitializeComponent();
            Presenter = new PushshiftApiPresenter();

            chkPeriodFilter_CheckedChanged(null, EventArgs.Empty);
            rdoDescending.Checked = true;
            ddlSortBy.DataSource = new string[] { "created_utc", "score" };
            txtTotalResults.Text = "100";

            SavedQueries = new string[0];
            SavedSubreddits = new string[0];
            SavedUserNames = new string[0];

            InitializeAutoCompleteSaveDir();
            PrePopulateFields();
        }

        #endregion

        #region Properties

        public bool IsPeriodSearchEnabled { get { return chkPeriodFilter.Checked; } }

        public string Query { get { return txtQuery.Text; } }

        public string ScoreGreaterThan { get { return txtGreaterThan.Text; } }
        public string ScoreLessThan { get { return txtLessThan.Text; } }
        public string SortDirection { get { return GetSortDirection(); } }

        public string SortType { get { return ddlSortBy.SelectedItem.ToString(); } }
        public long StartDateUnixTimeStamp { get { return ToUnixTime(dpStartDate.Value); } }
        public long StopDateUnixTimeStamp { get { return ToUnixTime(dpStopDate.Value); } }
        public string Subreddit { get { return txtSubreddit.Text; } }

        public string TotalResults { get { return txtTotalResults.Text; } }

        public string UserName { get { return txtUserName.Text; } }

        private string AutoCompleteSaveDir { get; set; }
        private string AutoCompleteQueryFilePath { get; set; }
        private string AutoCompleteSubredditFilePath { get; set; }
        private string AutoCompleteUserNameFilePath { get; set; }

        private PushshiftApiPresenter Presenter { get; set; }

        string[] SavedQueries { get; set; }
        string[] SavedSubreddits { get; set; }
        string[] SavedUserNames { get; set; }

        #endregion

        #region Event Handlers

        private void rtbResponse_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            string browserName = GetSystemDefaultBrowser();
            if (string.IsNullOrEmpty(browserName))
            {
                MessageBox.Show("Could not determine the default browser", "ERROR");
                return;
            }

            Process.Start(browserName, e.LinkText);
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            ToggleControls(false);
            SaveAutoCompletes();

            await Presenter.BuildResponseContent(this);
            lblCounter.Text = Presenter.Counter;
            rtbResponse.Text = Presenter.Response;

            ToggleControls(true);
        }

        private void chkPeriodFilter_CheckedChanged(object sender, EventArgs e)
        {
            tblFilterByPeriod.Enabled = chkPeriodFilter.Checked;
        }

        private void rdoFilterByScoreSortOrder_CheckedChanged(object sender, EventArgs e)
        {
            rdoAscending.Checked = !rdoDescending.Checked;
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSearch_Click(sender, e);
            }
        }

        #endregion

        #region Helper Methods

        private string GetSortDirection()
        {
            if (rdoAscending.Checked)
            {
                return "asc";
            }
            else if (rdoDescending.Checked)
            {
                return "desc";
            }
            else
            {
                return "asc";
            }
        }

        private string GetSystemDefaultBrowser()
        {
            // based on https://stackoverflow.com/a/17599201

            const string userChoice = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
            string progId;
            using (RegistryKey userChoiceKey = Registry.CurrentUser.OpenSubKey(userChoice))
            {
                if (userChoiceKey == null)
                {
                    return string.Empty;
                }

                object progIdValue = userChoiceKey.GetValue("Progid");
                if (progIdValue == null)
                {
                    return string.Empty;
                }

                progId = progIdValue.ToString();
                switch (progId)
                {
                    case "IE.HTTP":
                        return "iexplore.exe";
                    case "FirefoxURL":
                        return "firefox.exe";
                    case "ChromeHTML":
                        return "chrome.exe";
                    case "OperaStable":
                        return "opera.exe";
                    case "SafariHTML":
                        return "safari.exe";
                    case "AppXq0fevzme2pys62n3e0fbqa7peapykr8v":
                        return "msedge.exe";
                    default:
                        return string.Empty;
                }
            }
        }

        private void ToggleControls(bool enable)
        {
            btnSearch.Enabled = enable;
            tblFilterByPeriod.Enabled = enable;
            tblFilterByPeriodCheckBox.Enabled = enable;
            tblFilterByScore.Enabled = enable;
            tblFilterByScoreLabel.Enabled = enable;
            tblGeneral.Enabled = enable;
            tblSortByDirection.Enabled = enable;
            tblSortByType.Enabled = enable;
            chkPeriodFilter_CheckedChanged(null, EventArgs.Empty);
        }

        private void InitializeAutoCompleteSaveDir()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AutoCompleteSaveDir = Path.Combine(appDataPath, "PushshiftAPI", "autocomplete");
            if (!Directory.Exists(AutoCompleteSaveDir))
            {
                Directory.CreateDirectory(AutoCompleteSaveDir);
            }

            AutoCompleteQueryFilePath = Path.Combine(AutoCompleteSaveDir, "query");
            AutoCompleteSubredditFilePath = Path.Combine(AutoCompleteSaveDir, "subreddit");
            AutoCompleteUserNameFilePath = Path.Combine(AutoCompleteSaveDir, "username");
        }

        private void PrePopulateFields()
        {
            if (File.Exists(AutoCompleteQueryFilePath))
            {
                SavedQueries = File.ReadAllLines(AutoCompleteQueryFilePath);
                txtQuery.Text = SavedQueries.LastOrDefault();

                SetAutoCompleteField(txtQuery, SavedQueries);
            }

            if (File.Exists(AutoCompleteUserNameFilePath))
            {
                SavedUserNames = File.ReadAllLines(AutoCompleteUserNameFilePath);
                txtUserName.Text = SavedUserNames.LastOrDefault();

                SetAutoCompleteField(txtUserName, SavedUserNames);
            }

            if (File.Exists(AutoCompleteSubredditFilePath))
            {
                SavedSubreddits = File.ReadAllLines(AutoCompleteSubredditFilePath);
                txtSubreddit.Text = SavedSubreddits.LastOrDefault();

                SetAutoCompleteField(txtSubreddit, SavedSubreddits);
            }
        }

        private void SaveAutoCompletes()
        {
            List<string> savedQueries = SavedQueries.ToList();
            if (savedQueries.Contains(txtQuery.Text))
            {
                savedQueries.Remove(txtQuery.Text);
            }

            savedQueries.Add(txtQuery.Text);
            SavedQueries = savedQueries.ToArray();
            File.WriteAllLines(AutoCompleteQueryFilePath, SavedQueries);

            List<string> savedUserNames = SavedUserNames.ToList();
            if (savedUserNames.Contains(txtUserName.Text))
            {
                savedUserNames.Remove(txtUserName.Text);
            }

            savedUserNames.Add(txtUserName.Text);
            SavedUserNames = savedUserNames.ToArray();
            File.WriteAllLines(AutoCompleteUserNameFilePath, SavedUserNames);

            List<string> savedSubreddits = SavedSubreddits.ToList();
            if (savedSubreddits.Contains(txtSubreddit.Text))
            {
                savedSubreddits.Remove(txtSubreddit.Text);
            }

            savedSubreddits.Add(txtSubreddit.Text);
            SavedSubreddits = savedSubreddits.ToArray();
            File.WriteAllLines(AutoCompleteSubredditFilePath, SavedSubreddits);
        }

        private void SetAutoCompleteField(TextBox textBox, string[] allowedTypes)
        {
            AutoCompleteStringCollection autoCompleteList = new AutoCompleteStringCollection();
            autoCompleteList.AddRange(allowedTypes);

            textBox.AutoCompleteCustomSource = autoCompleteList;
            textBox.AutoCompleteMode = AutoCompleteMode.Suggest;
            textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
        }

        private long ToUnixTime(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - START_DATE).TotalSeconds;
        }

        #endregion

    }

}
