using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

namespace CriaArticulacaoInpho
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        List<EntradaArquivoCSV> ArquivoCSV = new List<EntradaArquivoCSV>();
        List<EntradaArquivoInpho> ArquivoInpho = new List<EntradaArquivoInpho>();

        public class EntradaArquivoCSV
        {
            public string X { get; set; }
            public string Y { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }
            public string Label { get; set; }
            public string Elevation { get; set; }
        }

        public class EntradaArquivoInpho
        {
            public string TitleID { get; set; }
            public string NWx { get; set; }
            public string NWy { get; set; }
            public string SEx { get; set; }
            public string SEy { get; set; }
        }

        public void gravaLog(string m)
        {
            File.AppendAllText("log.txt", "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "]\t" + m + "\r\n");
        }

        public MainWindow()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US"); // Utilizar ponto como separador decimal ao invés de vírgula
            InitializeComponent();

            gravaLog("==========================");
            gravaLog("= CRIA ARTICULAÇÃO INPHO =\tVersão " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            gravaLog("==========================");
            gravaLog("Inicializando programa...");

            dgCSV.ItemsSource = ArquivoCSV;
            dgInpho.ItemsSource = ArquivoInpho;


        }

        private void miCarregarCSV_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "Arquivo CSV (*.csv)|*.csv";
            d.ShowDialog();

            if (d.FileName == "") return;

            string arqCSV = d.FileName;
            gravaLog("Abrindo arquivo CSV: " + arqCSV);
            ArquivoCSV.Clear();
            ArquivoInpho.Clear();

            try
            {
                using (var arquivo = new StreamReader(arqCSV)) // Abre arquivo
                {
                    string linha;
                    int contOK = 0;
                    int contINV = 0;
                    int contBRA = 0;
                    bool labelEmBranco = false;

                    while ((linha = arquivo.ReadLine()) != null) // Lê linha por linha
                    {
                        if (linha.Length > 0)
                        {
                            char[] validChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }; // Caracteres válidos para início de linha

                            if (validChars.Contains(linha[0]))
                            {
                                string[] split = linha.Split(',');
                                ArquivoCSV.Add(new EntradaArquivoCSV() { X = split[0], Y = split[1], Latitude = split[2], Longitude = split[3], Label = split[4], Elevation = split[5] });
                                contOK++;
                                labelEmBranco = (split[4] == ""); // Verifica se existe algum label em branco para avisar o usuário no final do processo
                            }
                            else
                            {
                                contINV++;
                            }
                        }
                        else
                        {
                            contBRA++;
                        }
                    }
                    gravaLog("Arquivo CSV lido! Linhas válidas: " + contOK + " / Em branco: " + contBRA + " / Inválidas: " + contINV);
                    tbStatusText.Text = "Arquivo CSV lido: " + contOK + " linhas válidas.";

                    dgCSV.Items.Refresh();
                    ArquivoInpho.Clear();
                    dgInpho.Items.Refresh();

                    btGerarInpho.IsEnabled = false;
                    btGerarScript.IsEnabled = false;

                    if (labelEmBranco)
                    {
                        System.Windows.MessageBox.Show("ATENÇÃO!\n\nExistem centros de folhas SEM identificação!\n\nVerifique o arquivo CSV e importe novamente, a fim de evitar a geração de folhas sem nomes.\n\nVocê pode continuar com o processo, mas podem ser geradas folhas duplicadas ou em coordenadas erradas.", "Atenção");
                        gravaLog("Foram identificados centro de folhas sem nomes!");
                    }
                }
                btCalcular.IsEnabled = true;
                tbDimensaoX.IsEnabled = true;
                tbDimensaoY.IsEnabled = true;
                tbTamanhoPixel.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Erro ao abrir arquivo:\n\n" + ex, "Erro");
                gravaLog("Erro ao abrir arquivo: " + ex);
                tbStatusText.Text = "Erro ao abrir arquivo! Para mais informações consulte o log.";

                btCalcular.IsEnabled = false;
                tbDimensaoX.IsEnabled = false;
                tbDimensaoY.IsEnabled = false;
                tbTamanhoPixel.IsEnabled = false;
                btGerarInpho.IsEnabled = false;
                btGerarScript.IsEnabled = false;

                ArquivoCSV.Clear();
                ArquivoInpho.Clear();
                dgCSV.Items.Refresh();
                dgInpho.Items.Refresh();

            }
        }

        private void miVerLog_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("log.txt");
        }

        private void miSair_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void wiMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            gravaLog("Fechando programa...");
        }

        private void btCalcular_Click(object sender, RoutedEventArgs e)
        {
            double tamanhoPixel;
            if (!Double.TryParse(tbTamanhoPixel.Text.Replace(",","."), out tamanhoPixel))
            {
                System.Windows.MessageBox.Show("Tamanho do pixel inválido", "Erro");
                return;
            }
            double dimensaoX;
            if (!Double.TryParse(tbDimensaoX.Text.Replace(",", "."), out dimensaoX))
            {
                System.Windows.MessageBox.Show("Dimensão X inválida", "Erro");
                return;
            }
            double dimensaoY;
            if (!Double.TryParse(tbDimensaoY.Text.Replace(",", "."), out dimensaoY))
            {
                System.Windows.MessageBox.Show("Dimensão Y inválida", "Erro");
                return;
            }
            double arred = 1 / tamanhoPixel;

            gravaLog("Calculando com tamanho de pixel de " + tamanhoPixel + "m e folhas com dimensão de " + dimensaoX + "x" + dimensaoY);

            int count = 0;

            for (int i = 0; i < ArquivoCSV.Count; i++)
            {
                double centroX = Convert.ToDouble(ArquivoCSV[i].X);
                double centroY = Convert.ToDouble(ArquivoCSV[i].Y);

                double centroXArred = Math.Round(centroX * arred) / arred;
                double centroYArred = Math.Round(centroY * arred) / arred;

                double x1 = centroXArred - (dimensaoX / 2);
                double y1 = centroYArred + (dimensaoY / 2);
                double x2 = centroXArred + (dimensaoX / 2);
                double y2 = centroYArred - (dimensaoY / 2);

                ArquivoInpho.Add(new EntradaArquivoInpho() { TitleID = ArquivoCSV[i].Label, NWx = Convert.ToString(x1), NWy = Convert.ToString(y1), SEx = Convert.ToString(x2), SEy = Convert.ToString(y2) });
                count++;
            }

            dgInpho.Items.Refresh();

            btGerarInpho.IsEnabled = true;
            btGerarScript.IsEnabled = true;

            tbStatusText.Text = count + " linhas calculadas.";
            gravaLog("Foram calculadas " + count + " linhas.");
        }

        private void btGerarInpho_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "Articulação Inpho (*.txt)|*.txt";
            d.ShowDialog();

            if (d.FileName == "") return;

            string pathToSaveFile = d.FileName;

            try
            {
                using (var arqInpho = new StreamWriter(pathToSaveFile))
                {
                    arqInpho.WriteLine(" \"TileID\" \"NWx\" \"NWy\" \"SEx\" \"SEy\" ");
                    int count = 0;

                    for (int i = 0; i < ArquivoInpho.Count; i++)
                    {
                        string linha = ArquivoInpho[i].TitleID + " " + ArquivoInpho[i].NWx + " " + ArquivoInpho[i].NWy + " " + ArquivoInpho[i].SEx + " " + ArquivoInpho[i].SEy;
                        arqInpho.WriteLine(linha);
                        count++;
                    }
                    gravaLog(count + " folhas exportadas para o Inpho com sucesso.");
                    tbStatusText.Text = count + " folhas exportadas para o Inpho com sucesso.";
                    System.Windows.MessageBox.Show(count + " folhas exportadas com sucesso no formato Inpho!", "Sucesso");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Erro ao tentar salvar o arquivo Inpho:\n\n", "Erro");
                gravaLog("Erro ao tentar salvar o arquivo Inpho: " + ex);
                tbStatusText.Text = "Erro ao salvar o arquivo Inpho! Consulte o log para informações.";
            }
        }

        private void btGerarScript_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "Script AutoCAD (*.scr)|*.scr";
            d.ShowDialog();

            if (d.FileName == "") return;

            string pathToSaveFile = d.FileName;

            try
            {
                using (var arqScript = new StreamWriter(pathToSaveFile))
                {
                    int count = 0;

                    for (int i = 0; i < ArquivoInpho.Count; i++)
                    {
                        string linha = "rec " + ArquivoInpho[i].NWx + "," + ArquivoInpho[i].NWy + " " + ArquivoInpho[i].SEx + "," + ArquivoInpho[i].SEy + " ";
                        arqScript.WriteLine(linha);
                        count++;
                    }
                    arqScript.WriteLine("");
                    gravaLog(count + " folhas exportadas para o script do AutoCAD com sucesso.");
                    tbStatusText.Text = count + " folhas exportadas para o script do AutoCAD com sucesso.";
                    System.Windows.MessageBox.Show(count + " folhas exportadas com sucesso no formato Script para AutoCAD!", "Sucesso");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Erro ao tentar salvar o arquivo script:\n\n", "Erro");
                gravaLog("Erro ao tentar salvar o arquivo script: " + ex);
                tbStatusText.Text = "Erro ao salvar o arquivo script! Consulte o log para informações.";
            }
        }

        private void miSobre_Click(object sender, RoutedEventArgs e)
        {
            string v = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string m = ("Gerador de Articulação para Inpho " + v + "\nBASE Aerofotogrametria e Projetos S.A.\n\nProgramação: Henrique Germano Miraldo");

            System.Windows.MessageBox.Show(m, "Sobre");
        }
    }
}
