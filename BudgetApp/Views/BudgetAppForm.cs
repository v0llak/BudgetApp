using BudgetApp.Models;
using BudgetApp.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace BudgetApp
{
    public partial class BudgetApp : Form
    {
        #region Initialise variables
        private static List<Transaction> transactionsList = new List<Transaction>();
        private static List<Category> categoriesList = new List<Category>();
        private static List<string> uniqueCategoriesList = new List<string>();
        #endregion

        public BudgetApp()
        {
            InitializeComponent();
            LoadDataFromDatabase();
            PopulateFormBoxes();
            PopulateCategoryGraph();
            PopulateMonthBarGraph();
        }
        public static void ReloadForm()
        {
            ///Runs when the App is first started and when when the FinishBtn on the ImportDataForm is clicked 
            
            LoadDataFromDatabase();
        }

        static void LoadDataFromDatabase()
        {
            transactionsList = SqliteDataAccess.LoadTransactions();
            categoriesList = SqliteDataAccess.LoadCategories();
            uniqueCategoriesList = SqliteDataAccess.LoadUniqueCategoryList();

            //Ensure user has the default categories
            Category.SaveDefaultCategories();
        }

        void Importbtn_Click(object sender, EventArgs e)
        {
            //Set the file name to null to begin with
            openFileDialog.FileName = "";
            //Restrict the file types we can attempt to open
            openFileDialog.Filter = "Excel File|*.xlsx;*.xls;*.csv";

            //If the user has selected a file and pressed 'Open' and not 'Cancel'
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //If the file name has now been updated from null to something
                //Open a new ImportDataForm and run the ImportData method on that file
                if (openFileDialog.FileName.Trim() != "")
                {
                    ImportDataForm importDataForm = new ImportDataForm();
                    importDataForm.Show();
                    importDataForm.ImportData(openFileDialog.FileName);
                }
            }
        }//Possibly move this to the import data form

        void PopulateFormBoxes()
        {
            if (transactionsList.Count > 0)
            {
                //Date Pickers
                //Sort the transations list by date
                transactionsList.Sort((i, j) => DateTime.Compare(i.Date, j.Date));
                //Set DateTimePickers dates
                FromDateTimePicker.Value = transactionsList.First().Date;
                ToDateTimePicker.Value = transactionsList.Last().Date;

                //YearComoboBox
                int currentYear = transactionsList.First().Date.Year;
                int lastYear = transactionsList.Last().Date.Year;
                //Add all the years between the first and last to the YearComboBox
                while (currentYear <= lastYear)
                {
                    YearComboBox.Items.Add(currentYear.ToString());
                    currentYear++;
                }
            }

            //Set the YearComboBox to the current year
            YearComboBox.Text = DateTime.Now.Year.ToString();
        }

        void PopulateMonthBarGraph()
        {
            //Clear values from the chart
            monthChart.Series["Income"].Points.Clear();
            monthChart.Series["Expenses"].Points.Clear();
            monthChart.Series["Net"].Points.Clear();

            //For each month add the month and the totals to the chart
            for (int i=1; i<=12; i++)
            {
                string month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i);
                DateTime monthStart = new DateTime(int.Parse(YearComboBox.Text), i, 1);
                DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1); //Get the 1st of the next month, then subtract 1 day

                double monthIncome = Transaction.Total(transactionsList, monthStart, monthEnd, "Income");
                double monthExpenses = Transaction.Total(transactionsList, monthStart, monthEnd, null) - monthIncome;
                double monthNet = monthIncome + monthExpenses;

                //Limit the chart to positive numbers
                if(monthNet < 0) monthNet = 0;
                monthExpenses *= -1;

                monthChart.Series["Income"].Points.AddXY(month, monthIncome);
                monthChart.Series["Expenses"].Points.AddY(monthExpenses);
                monthChart.Series["Net"].Points.AddY(monthNet);
            }
        }

        void YearComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            PopulateMonthBarGraph();

            DateTime yearStart = new DateTime(int.Parse(YearComboBox.Text), 1, 1);
            DateTime yearEnd = yearStart.AddMonths(13).AddDays(-1);

            double income = Transaction.Total(transactionsList, yearStart, yearEnd, "Income");
            double expenses = Transaction.Total(transactionsList, yearStart, yearEnd, null) - income;

            YearIncomeValue.Text = income.ToString("0.##");
            YearExpensesValue.Text = (expenses*-1).ToString("0.##");
            YearTotalValue.Text = (expenses + income).ToString("0.##");
        }

        #region DateTime Pickers

        void FromDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            PopulateDateTotals();
            PopulateCategoryGraph();
        }

        void ToDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            PopulateDateTotals();
            PopulateCategoryGraph();
        }

        void PopulateDateTotals()
        {
            PopulateCategoryGraph();

            double income = Transaction.Total(transactionsList, FromDateTimePicker.Value, ToDateTimePicker.Value, "Income");
            double expenses = Transaction.Total(transactionsList, FromDateTimePicker.Value, ToDateTimePicker.Value, null) - income;

            IncomeValue.Text = income.ToString("0.##");
            ExpensesValue.Text = (expenses * -1).ToString("0.##");
            NetValue.Text = (expenses + income).ToString("0.##");
        }

        void PopulateCategoryGraph()
        {
            //Clear values from the chart
            categoryExpencesChart.Series["Category Totals"].Points.Clear();

            //Add the totals per category to the chart, Except for the 'Ignore' and 'Income' Categories
            foreach (string categoryName in uniqueCategoriesList)
            {
                double categoryTotal = Transaction.Total(transactionsList, FromDateTimePicker.Value, ToDateTimePicker.Value, categoryName);

                //Expenses
                if (categoryTotal != 0 && categoryName != "Ignore" && categoryName != "Income")
                {
                    categoryExpencesChart.Series["Category Totals"].Points.AddXY(categoryName, categoryTotal * -1);
                }
            }
        }

        #endregion
    }
}
