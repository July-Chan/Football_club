using MySql.Data.MySqlClient;
using System.Data;

namespace Football
{
    public partial class Form1 : Form
    {
        string connectionString = "Server=localhost;Database=football_club;Uid=root;Pwd=12345678;";

        public Form1()
        {
            InitializeComponent();
            LoadTableList();
        }

        private void LoadTableList()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SHOW TABLES";
                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                    DataTable tableList = new DataTable();
                    adapter.Fill(tableList);

                    comboBox1.DataSource = tableList;
                    comboBox1.DisplayMember = "Tables_in_football_club";
                    comboBox1.ValueMember = "Tables_in_football_club";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }

        private void LoadData(string tableName)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = $"SELECT * FROM {tableName}"; 
                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    BindingSource bindingSource = new BindingSource();
                    bindingSource.DataSource = table;
                    dataGridView1.DataSource = bindingSource;

                    dataGridView1.AutoGenerateColumns = true;
                    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadTableList();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string selectedTable = comboBox1.SelectedValue.ToString();
            LoadData(selectedTable);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    dataGridView1.Rows.Remove(row);
                }
            }
            else
            {
                MessageBox.Show("Виберіть рядок для видалення.");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView1.DataSource is BindingSource bindingSource)
            {
                DataTable table = bindingSource.DataSource as DataTable;
                if (table != null)
                {
                    DataRow newRow = table.NewRow();
                    table.Rows.Add(newRow);
                    bindingSource.EndEdit();
                    dataGridView1.Refresh();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.DataSource is BindingSource bindingSource)
                {
                    DataTable table = bindingSource.DataSource as DataTable;
                    if (table != null)
                    {
                        using (MySqlConnection connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            MySqlDataAdapter adapter = new MySqlDataAdapter($"SELECT * FROM {comboBox1.SelectedValue}", connection);

                            MySqlCommandBuilder commandBuilder = new MySqlCommandBuilder(adapter);
                            adapter.UpdateCommand = commandBuilder.GetUpdateCommand();
                            adapter.InsertCommand = commandBuilder.GetInsertCommand();
                            adapter.DeleteCommand = commandBuilder.GetDeleteCommand();

                            adapter.Update(table);
                            MessageBox.Show("Зміни успішно збережено до бази даних.");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Дані не прив'язані до таблиці.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                string selectedTable = comboBox1.SelectedValue.ToString();
                string searchQuery = textBox1.Text; 

                if (string.IsNullOrEmpty(searchQuery))
                {
                    MessageBox.Show("Введіть текст для пошуку.");
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $"SELECT * FROM {selectedTable} WHERE " +
                                   string.Join(" OR ", GetTextColumnConditions(selectedTable, connection, searchQuery));

                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                    DataTable searchResults = new DataTable();
                    adapter.Fill(searchResults);

                    if (searchResults.Rows.Count > 0)
                    {
                        dataGridView1.DataSource = searchResults;
                    }
                    else
                    {
                        MessageBox.Show("Результатів не знайдено.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка пошуку: {ex.Message}");
            }
        }

        private List<string> GetTextColumnConditions(string tableName, MySqlConnection connection, string searchQuery)
        {
            List<string> conditions = new List<string>();

            string columnQuery = $"SHOW COLUMNS FROM {tableName}";
            MySqlCommand command = new MySqlCommand(columnQuery, connection);
            MySqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string columnName = reader["Field"].ToString();
                string dataType = reader["Type"].ToString();

                if (dataType.Contains("varchar") || dataType.Contains("text"))
                {
                    conditions.Add($"{columnName} LIKE '%{searchQuery}%'");
                }
                else if (dataType.Contains("int") || dataType.Contains("decimal") || dataType.Contains("float") || dataType.Contains("double"))
                {
                    if (decimal.TryParse(searchQuery, out _))
                    {
                        conditions.Add($"{columnName} = {searchQuery}");
                    }
                }
                else if (dataType.Contains("enum"))
                {
                    if (columnName == "Result")
                    {
                        if (searchQuery.Equals("win", StringComparison.OrdinalIgnoreCase) ||
                            searchQuery.Equals("draw", StringComparison.OrdinalIgnoreCase) ||
                            searchQuery.Equals("loss", StringComparison.OrdinalIgnoreCase))
                        {
                            conditions.Add($"{columnName} = '{searchQuery}'");
                        }
                    }
                }
            }
            reader.Close();

            return conditions;
        }

    }
}
