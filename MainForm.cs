using System.Drawing;
using System.Windows.Forms;
using ShieldGuard.Core;

namespace ShieldGuard;

public class MainForm : Form
{
    private Panel sidebar = null!;
    private Panel mainArea = null!;
    private Panel header = null!;
    private Panel dashboard = null!;

    private Label statusText = null!;
    private Label protectionStatus = null!;
    private Label blockedAdsValue = null!;
    private Label browsersValue = null!;
    private Label systemValue = null!;
    private Label filtersValue = null!;
    private Button protectionButton = null!;

    private readonly ProtectionManager protectionManager;

    private readonly System.Windows.Forms.Timer dashboardTimer;

    private readonly Color backgroundColor =
        Color.FromArgb(15, 17, 23);

    private readonly Color sidebarColor =
        Color.FromArgb(20, 23, 30);

    private readonly Color cardColor =
        Color.FromArgb(25, 29, 38);

    private readonly Color primaryColor =
        Color.FromArgb(74, 144, 226);

    private readonly Color textColor =
        Color.FromArgb(235, 238, 245);

    private readonly Color secondaryTextColor =
        Color.FromArgb(145, 153, 168);

    public MainForm()
    {
        protectionManager =
            new ProtectionManager();

        dashboardTimer =
            new System.Windows.Forms.Timer
            {
                Interval = 1000
            };

        dashboardTimer.Tick +=
            DashboardTimer_Tick;

        InitializeForm();
        BuildInterface();

        dashboardTimer.Start();

        FormClosing +=
            MainForm_FormClosing;
    }

    private void InitializeForm()
    {
        Text = "ShieldGuard";

        StartPosition =
            FormStartPosition.CenterScreen;

        MinimumSize =
            new Size(
                1000,
                650);

        Size =
            new Size(
                1200,
                750);

        BackColor =
            backgroundColor;

        ForeColor =
            textColor;

        Font =
            new Font(
                "Segoe UI",
                10F);

        FormBorderStyle =
            FormBorderStyle.Sizable;

        DoubleBuffered = true;
    }

    private void BuildInterface()
    {
        BuildSidebar();
        BuildMainArea();
    }

    private void BuildSidebar()
    {
        sidebar =
            new Panel
            {
                Dock =
                    DockStyle.Left,

                Width = 230,

                BackColor =
                    sidebarColor,

                Padding =
                    new Padding(
                        18,
                        20,
                        18,
                        20)
            };

        Controls.Add(
            sidebar);

        Label logo =
            new Label
            {
                Text =
                    "SHIELDGUARD",

                Dock =
                    DockStyle.Top,

                Height = 55,

                Font =
                    new Font(
                        "Segoe UI Semibold",
                        16F),

                ForeColor =
                    textColor,

                TextAlign =
                    ContentAlignment.MiddleLeft
            };

        Panel menu =
            new Panel
            {
                Dock =
                    DockStyle.Top,

                Height = 250
            };

        sidebar.Controls.Add(
            menu);

        sidebar.Controls.Add(
            logo);

        AddMenuButton(
            menu,
            "Dashboard",
            0,
            true);

        AddMenuButton(
            menu,
            "Qorunma",
            52,
            false);

        AddMenuButton(
            menu,
            "Statistika",
            104,
            false);

        AddMenuButton(
            menu,
            "Filtrlər",
            156,
            false);

        AddMenuButton(
            menu,
            "Parametrlər",
            208,
            false);

        Label version =
            new Label
            {
                Text =
                    "ShieldGuard v1.0.0",

                Dock =
                    DockStyle.Bottom,

                Height = 30,

                ForeColor =
                    secondaryTextColor,

                Font =
                    new Font(
                        "Segoe UI",
                        8.5F),

                TextAlign =
                    ContentAlignment.MiddleLeft
            };

        sidebar.Controls.Add(
            version);
    }

    private void AddMenuButton(
        Panel parent,
        string text,
        int top,
        bool active)
    {
        Button button =
            new Button
            {
                Text = text,

                Left = 0,

                Top = top,

                Width =
                    parent.Width,

                Height = 44,

                FlatStyle =
                    FlatStyle.Flat,

                BackColor =
                    active
                        ? Color.FromArgb(
                            32,
                            38,
                            49)
                        : Color.Transparent,

                ForeColor =
                    active
                        ? textColor
                        : secondaryTextColor,

                Font =
                    new Font(
                        "Segoe UI",
                        10F),

                TextAlign =
                    ContentAlignment.MiddleLeft,

                Padding =
                    new Padding(
                        14,
                        0,
                        0,
                        0),

                Cursor =
                    Cursors.Hand
            };

        button.FlatAppearance
            .BorderSize = 0;

        button.FlatAppearance
            .MouseOverBackColor =
            Color.FromArgb(
                30,
                35,
                45);

        parent.Controls.Add(
            button);
    }

    private void BuildMainArea()
    {
        mainArea =
            new Panel
            {
                Dock =
                    DockStyle.Fill,

                BackColor =
                    backgroundColor,

                Padding =
                    new Padding(28)
            };

        Controls.Add(
            mainArea);

        mainArea.BringToFront();

        BuildHeader();
        BuildDashboard();
    }

    private void BuildHeader()
    {
        header =
            new Panel
            {
                Dock =
                    DockStyle.Top,

                Height = 70,

                BackColor =
                    backgroundColor
            };

        mainArea.Controls.Add(
            header);

        Label title =
            new Label
            {
                Text =
                    "Dashboard",

                Left = 0,

                Top = 5,

                Width = 500,

                Height = 35,

                Font =
                    new Font(
                        "Segoe UI Semibold",
                        21F),

                ForeColor =
                    textColor
            };

        header.Controls.Add(
            title);

        Label subtitle =
            new Label
            {
                Text =
                    "ShieldGuard sisteminin ümumi vəziyyəti",

                Left = 2,

                Top = 40,

                Width = 500,

                Height = 25,

                Font =
                    new Font(
                        "Segoe UI",
                        9.5F),

                ForeColor =
                    secondaryTextColor
            };

        header.Controls.Add(
            subtitle);
    }

    private void BuildDashboard()
    {
        dashboard =
            new Panel
            {
                Dock =
                    DockStyle.Fill,

                BackColor =
                    backgroundColor,

                AutoScroll = true
            };

        mainArea.Controls.Add(
            dashboard);

        dashboard.BringToFront();

        BuildStatusCard();
        BuildStatisticsCards();
    }

    private void BuildStatusCard()
    {
        Panel card =
            new Panel
            {
                Left = 0,

                Top = 10,

                Width = 700,

                Height = 210,

                BackColor =
                    cardColor
            };

        dashboard.Controls.Add(
            card);

        Label title =
            new Label
            {
                Text =
                    "Qorunma statusu",

                Left = 25,

                Top = 22,

                Width = 400,

                Height = 30,

                Font =
                    new Font(
                        "Segoe UI Semibold",
                        14F),

                ForeColor =
                    textColor
            };

        card.Controls.Add(
            title);

        protectionStatus =
            new Label
            {
                Text =
                    "DEAKTİV",

                Left = 25,

                Top = 62,

                Width = 200,

                Height = 35,

                Font =
                    new Font(
                        "Segoe UI Semibold",
                        17F),

                ForeColor =
                    Color.FromArgb(
                        230,
                        100,
                        100)
            };

        card.Controls.Add(
            protectionStatus);

        statusText =
            new Label
            {
                Text =
                    "Qorunma mühərriki başladılmayıb",

                Left = 25,

                Top = 105,

                Width = 500,

                Height = 25,

                Font =
                    new Font(
                        "Segoe UI",
                        9.5F),

                ForeColor =
                    secondaryTextColor
            };

        card.Controls.Add(
            statusText);

        protectionButton =
            new Button
            {
                Text =
                    "QORUNMANI BAŞLAT",

                Left = 25,

                Top = 145,

                Width = 220,

                Height = 42,

                FlatStyle =
                    FlatStyle.Flat,

                BackColor =
                    primaryColor,

                ForeColor =
                    Color.White,

                Font =
                    new Font(
                        "Segoe UI Semibold",
                        9.5F),

                Cursor =
                    Cursors.Hand
            };

        protectionButton
            .FlatAppearance
            .BorderSize = 0;

        protectionButton.Click +=
            ProtectionButton_Click;

        card.Controls.Add(
            protectionButton);
    }

    private void BuildStatisticsCards()
    {
        CreateStatisticCard(
            "Bloklanan reklamlar",
            "0",
            0,
            220,
            out blockedAdsValue);

        CreateStatisticCard(
            "Brauzerlər",
            protectionManager
                .GetBrowserCount()
                .ToString(),
            240,
            220,
            out browsersValue);

        CreateStatisticCard(
            "Filterlər",
            "0",
            480,
            220,
            out filtersValue);

        CreateStatisticCard(
            "Sistem",
            "Normal",
            720,
            220,
            out systemValue);

        systemValue.ForeColor =
            Color.FromArgb(
                100,
                210,
                140);
    }

    private void CreateStatisticCard(
        string title,
        string value,
        int left,
        int width,
        out Label valueLabel)
    {
        Panel card =
            new Panel
            {
                Left = left,

                Top = 240,

                Width = width,

                Height = 125,

                BackColor =
                    cardColor
            };

        dashboard.Controls.Add(
            card);

        Label titleLabel =
            new Label
            {
                Text =
                    title,

                Left = 20,

                Top = 18,

                Width =
                    width - 40,

                Height = 25,

                Font =
                    new Font(
                        "Segoe UI",
                        9F),

                ForeColor =
                    secondaryTextColor
            };

        card.Controls.Add(
            titleLabel);

        valueLabel =
            new Label
            {
                Text =
                    value,

                Left = 20,

                Top = 48,

                Width =
                    width - 40,

                Height = 45,

                Font =
                    new Font(
                        "Segoe UI Semibold",
                        22F),

                ForeColor =
                    textColor
            };

        card.Controls.Add(
            valueLabel);
    }

    private void ProtectionButton_Click(
        object? sender,
        EventArgs e)
    {
        if (!protectionManager.IsRunning)
        {
            StartProtection();
        }
        else
        {
            StopProtection();
        }
    }

    private void StartProtection()
    {
        try
        {
            protectionManager.Start();

            protectionStatus.Text =
                "AKTİV";

            protectionStatus.ForeColor =
                Color.FromArgb(
                    100,
                    210,
                    140);

            statusText.Text =
                "Qorunma mühərriki aktivdir";

            protectionButton.Text =
                "QORUNMANI DAYANDIR";

            protectionButton.BackColor =
                Color.FromArgb(
                    190,
                    75,
                    75);

            filtersValue.Text =
                protectionManager
                    .FilterCount
                    .ToString();

            browsersValue.Text =
                protectionManager
                    .GetBrowserCount()
                    .ToString();

            blockedAdsValue.Text =
                protectionManager
                    .BlockedCount
                    .ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "ShieldGuard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void StopProtection()
    {
        protectionManager.Stop();

        protectionStatus.Text =
            "DEAKTİV";

        protectionStatus.ForeColor =
            Color.FromArgb(
                230,
                100,
                100);

        statusText.Text =
            "Qorunma mühərriki dayandırılıb";

        protectionButton.Text =
            "QORUNMANI BAŞLAT";

        protectionButton.BackColor =
            primaryColor;

        blockedAdsValue.Text =
            "0";

        filtersValue.Text =
            "0";
    }

    private void DashboardTimer_Tick(
        object? sender,
        EventArgs e)
    {
        if (!protectionManager.IsRunning)
        {
            return;
        }

        blockedAdsValue.Text =
            protectionManager
                .BlockedCount
                .ToString();

        filtersValue.Text =
            protectionManager
                .FilterCount
                .ToString();

        browsersValue.Text =
            protectionManager
                .GetBrowserCount()
                .ToString();
    }

    private void MainForm_FormClosing(
        object? sender,
        FormClosingEventArgs e)
    {
        dashboardTimer.Stop();

        protectionManager.Dispose();
    }
}