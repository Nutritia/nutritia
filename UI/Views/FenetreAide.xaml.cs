﻿using System;
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
using System.IO;
using System.Windows.Resources;

namespace Nutritia.UI.Views
{
    /// <summary>
    /// Interaction logic for FenetreAide.xaml
    /// </summary>
    public partial class FenetreAide : Window
    {
        public FenetreAide(String nomFenetre)
        {
            InitializeComponent();

            switch (nomFenetre)
            {
                case "Bannissement":
                    tcAide.SelectedItem = tiSuspenssion;
                    break;
                case "MenuPrincipal":
                    tcAide.SelectedItem = tiMenuPrincipal;
                    break;
                case "FenetreConnexion":
                    tcAide.SelectedItem = tiConnexion;
                    break;
                case "AjoutPlat":
                    tcAide.SelectedItem = tiAjoutPlat;
                    break;
                case "CreationProfil":
                    tcAide.SelectedItem = tiProfil;
                    break;
                case "FenetreCalculatriceNutritionelle":
                    tcAide.SelectedItem = tiCalculatrice;
                    break;
                case "FenetreVotes":
                    tcAide.SelectedItem = tiVotes;
                    break;
                case "FenetreGenerateurMenus":
                    tcAide.SelectedItem = tiGenerationMenu;
                    break;
                case "FenetreListeEpicerie":
                    tcAide.SelectedItem = tiListeEpicerie;
                    break;
                case "MenuPrincipalConnecte":
                    tcAide.SelectedItem = tiMenuConnecte;
                    break;
                case "ModificationProfil":
                    tcAide.SelectedItem = tiProfil;
                    break;
                case "FenetreMenuAdministrateur":
                    tcAide.SelectedItem = tiMenuAdmin;
                    break;
                case "GestionRepertoire":
                    tcAide.SelectedItem = tiGestionAlimentsPlats;
                    break;
                case "FenetreDons":
                    tcAide.SelectedItem = tiAdminDons;
                    break;
                case "FenetreGestionAdmin":
                    tcAide.SelectedItem = tiGestionAdmins;
                    break;
                default:
                    tcAide.SelectedItem = tiNutritia;
                    break;
            }

            AppliquerText();

            // On redéfinnit la hauteur de la fenêtre suivant le nombre d'onglets
            //this.Height = 25 + tcAide.Items.Count * 31;

        }

        /// <summary>
        /// Transverse le texte contenu dans le fichier texte a la bonne section jusqu'a l'écran
        /// </summary>
        void AppliquerText()
        {
            // Placer tous le contenu du fichier en mémoire dans une liste de ligne
            StreamResourceInfo streamResourceInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Aide/FichierAide.txt"));
            StreamReader sr = new StreamReader(streamResourceInfo.Stream);
            List<String> lstLigne = sr.ReadToEnd().Split(Environment.NewLine.ToCharArray()).ToList();
            sr.Dispose();

            Dictionary<String, String> dicAide = new Dictionary<String, String>();
            String section = "";
            List<String> lstSting = new List<string>();

            foreach (var ligne in lstLigne)
            {
                if (ligne.Contains("#"))
                {
                    section = ligne.Replace("#", "");
                    dicAide.Add(section, "");
                    lstSting.Add(section);
                }
                else
                {
                    dicAide[section] += ligne + "\n";
                }

            }

            foreach (var texte in dicAide)
            {
                TextBlock tbContenuAide = (TextBlock)this.FindName(texte.Key);
                if (tbContenuAide != null)
                    tbContenuAide.Text = texte.Value;
                else
                {
                    TabItem tiNvlSection = new TabItem();
                    tiNvlSection.Style = (Style)FindResource("tabItem");
                    tiNvlSection.Header = texte.Key;
                    ScrollViewer svNvlSection = new ScrollViewer();
                    svNvlSection.PreviewMouseWheel += ScrollFocus;
                    StackPanel spNvlSection = new StackPanel();

                    Label lblTitre = new Label();
                    lblTitre.Content = texte.Key;
                    lblTitre.Style = (Style)FindResource("fontSousTitre");
                    TextBlock tbContenuNvSection = new TextBlock();
                    tbContenuNvSection.TextWrapping = TextWrapping.Wrap;
                    tbContenuNvSection.Text = texte.Value;

                    // On ajoute tous les enfants dans les parents
                    spNvlSection.Children.Add(lblTitre);
                    spNvlSection.Children.Add(tbContenuNvSection);

                    // On définnit ensuite les valeur de contenu
                    svNvlSection.Content = spNvlSection;
                    tiNvlSection.Content = svNvlSection;

                    // Puis on ajoute finalement cet onglet au TabControl
                    tcAide.Items.Add(tiNvlSection);
                }
            }
        }

        /// <summary>
        /// Code de Guillaume (légerement modifié pour qu'il soit compatible avec un apel de tous les SV):
        /// Événement lancé lorsque la roulette de la souris est utilisée dans le "scrollviewer" contenant le menu.
        /// Explicitement, cet événement permet de gérer le "scroll" avec la roulette correctement sur toute la surface du "scrollviewer".
        /// Si on ne le gère pas, il est seulement possible de "scroller" lorsque le pointeur de la souris est situé sur la "scrollbar".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollFocus(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        }
    }
}