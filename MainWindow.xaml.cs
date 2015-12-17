﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Nutritia.UI.Views;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using Infralution.Localization.Wpf;

namespace Nutritia
{
	/// <summary>
	/// Logique d'interaction pour MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, IApplicationService
	{
		private const int SLEEP_NOTIFICATION_TIME = 500;
		// Thread asyncrone qui surveille les modifications dans la BD
		private Thread ThreadVerifChangement { get; set; }

		// Permet d'interargir avec le threads asyncrones pour sauvegarder les nouveaux plats dans la BD
		static public List<Plat> NouveauxPlats { get; set; }
		public List<Plat> NvPlatAffichable { get; set; }
		public string MessageNouvelleVersion { get; set; }

		private IPlatService platAsync;
		private IMembreService membreAsync;
		public List<Plat> LstPlat { get; set; }

		private DateTime previousTime;
		private DateTime currentTime;

		VersionLogiciel latestVersionLogiciel;

		static bool debug = false;

		public MainWindow()
		{
			CultureManager.UICulture = new CultureInfo(App.LangueInstance.IETF);
			previousTime = DateTime.MinValue;

			InitializeComponent();
			ConfigurerTaille();
			Configurer();
			platAsync = new MySqlPlatService();
			membreAsync = new MySqlMembreService();
			MessageNouvelleVersion = "";
			NouveauxPlats = new List<Plat>();
			NvPlatAffichable = new List<Plat>();
			LstPlat = ServiceFactory.Instance.GetService<IPlatService>().RetrieveAll().ToList();

			presenteurContenu.Content = new MenuPrincipal();

			// On lance le thread
			ThreadVerifChangement = new Thread(VerifierChangementBD);
			ThreadVerifChangement.IsBackground = true;
			ThreadVerifChangement.Start();
		}


		/// <summary>
		/// Méthode appellé en asyncrone qui boucle toute les demi-secondes pour verifier s'il y a des changements dans la BD
		/// </summary>
		private void VerifierChangementBD()
		{
			// Permet de comparer pour detecter les changements
			List<Plat> lstPlatAJour;
			Membre membreAJour;

			while (true)
			{
				if (!String.IsNullOrEmpty(App.MembreCourant.NomUtilisateur))
				{
					// Mise a jour des variables
					//Faudrait juste qui vérifie le temps de mise à jour du membre.
					membreAJour = membreAsync.Retrieve(new RetrieveMembreArgs { NomUtilisateur = App.MembreCourant.NomUtilisateur });

					currentTime = platAsync.LastUpdatedTime();
					if (currentTime > previousTime)
					{
						previousTime = currentTime;

						lstPlatAJour = platAsync.RetrieveAll().ToList();

						//Raffraichi la liste dans la page des votes lorsqu'un autre utilisateur vote
						LstPlat = lstPlatAJour;
						Dispatcher.Invoke(AppliquerNouveauChangementStatut);


						// Check si de nouveaux plats sont diponible pour un utilisateur (qui n'a pas encore vu la notif)
						foreach (var plat in lstPlatAJour)// On check donc la date de mise a jour du membre
							if (plat.DerniereMaj > App.MembreCourant.DerniereMaj)
								NouveauxPlats.Add(plat);
							

						if (NouveauxPlats.Count > 0) // Si le systeme a trouvé des plats plus récent que la 
						{							// date de mise a jour du membre.
							NvPlatAffichable = new List<Plat>(NouveauxPlats); // On stocke ces plats dans une
							NouveauxPlats.Clear();							// liste pour les afficher
							Dispatcher.Invoke(DessinerNotificationNvPlat);
						}
					}

					// Checker si l'utilisateur est rendu banni
					if (membreAJour.EstBanni != App.MembreCourant.EstBanni)
					{
						if (membreAJour.NomUtilisateur == App.MembreCourant.NomUtilisateur)
						{
							App.MembreCourant = membreAJour.Cloner();
							// Si oui, on applique le changement
							Dispatcher.Invoke(AppliquerNouveauChangementStatut);
							// Et on met à jour le membre courrant
						}

					}

					// Checker si un utilisateur est devenu admin ou ne l'est plus
					if (membreAJour.EstAdministrateur != App.MembreCourant.EstAdministrateur)
					{
						if (membreAJour.NomUtilisateur == App.MembreCourant.NomUtilisateur)
						{
							App.MembreCourant = membreAJour.Cloner();
							// Si oui, on applique le changement
							Dispatcher.Invoke(AppliquerNouveauChangementStatut);
							// Et on met à jour le membre courrant
						}

					}

				}
				else
				{
					// Lorsque l'utilisateur se déconnecte ou qu'il n'est pas encore connecté,
					// On efface les plats s'il y en a afin d'hoter la notification de nouveau plat
					// Et on force la mise à jour de la notif
					if (NvPlatAffichable.Count > 0)
						Dispatcher.Invoke(DessinerNotificationNvPlat);
				}

				// VERIFICATION DE LA NOUVELLE VERSION DU LOGICIEL

				IVersionLogicielService serviceMembre = ServiceFactory.Instance.GetService<IVersionLogicielService>();

				latestVersionLogiciel = serviceMembre.RetrieveLatest();

				//Ne devrait pas s'exécuter
				if (String.IsNullOrEmpty(latestVersionLogiciel.Version))
				{
					return;
				}

				Version versionBD = new Version(latestVersionLogiciel.Version);
				Version versionLocal = new Version(FileVersionInfo.GetVersionInfo(App.ResourceAssembly.Location).FileVersion);

				if (versionBD > versionLocal && MessageNouvelleVersion == "")
				{
					MessageNouvelleVersion = "Version: " + latestVersionLogiciel.Version + " disponible.";
					Dispatcher.Invoke(DessinerNotificationNvnvVersion);
				}

				Thread.Sleep(App.POOL_TIME);
			}
		}

		private void ConfigurerTaille()
		{
			Width = App.APP_WIDTH;
			Height = App.APP_HEIGHT;
			MinWidth = App.APP_WIDTH;
			MinHeight = App.APP_HEIGHT;
		}

		/// <summary>
		/// Charge toutes les ressources du service factory
		/// </summary>
		public void Configurer()
		{
			// Inscription des différents services de l'application dans le ServiceFactory.
			ServiceFactory.Instance.Register<IRestrictionAlimentaireService, MySqlRestrictionAlimentaireService>(new MySqlRestrictionAlimentaireService());
			ServiceFactory.Instance.Register<IObjectifService, MySqlObjectifService>(new MySqlObjectifService());
			ServiceFactory.Instance.Register<IPreferenceService, MySqlPreferenceService>(new MySqlPreferenceService());
			ServiceFactory.Instance.Register<IAlimentService, MySqlAlimentService>(new MySqlAlimentService());
			ServiceFactory.Instance.Register<IPlatService, MySqlPlatService>(new MySqlPlatService());
			ServiceFactory.Instance.Register<ISuiviPlatService, MySqlSuiviPlatService>(new MySqlSuiviPlatService());
			ServiceFactory.Instance.Register<IMenuService, MySqlMenuService>(new MySqlMenuService());
			ServiceFactory.Instance.Register<IMembreService, MySqlMembreService>(new MySqlMembreService());
			ServiceFactory.Instance.Register<IVersionLogicielService, MySqlVersionLogicielService>(new MySqlVersionLogicielService());
			ServiceFactory.Instance.Register<IApplicationService, MainWindow>(this);
			ServiceFactory.Instance.Register<IDonService, MySqlDonService>(new MySqlDonService());
		}

		/// <summary>
		/// Méthode qui permet de changer de userControl
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="vue"></param>
		public void ChangerVue<T>(T vue)
		{
			presenteurContenu.Content = vue as UserControl;
		}		

		/// <summary>
		/// Méthode appelé par le thread des changements pour:
		/// - Expulser un utilisateur nouvellement banni
		/// - Expulser un utilisateur de l'environnement admin qui a perdu le statut d'admin
		/// - Actualiser le menu principal d'un utilisateur connécté en cas de modif du statut admin
		/// - Actualiser la liste des plats dans la fenêtre de vote lors d'un nouveau vote
		/// </summary>
		public void AppliquerNouveauChangementStatut()
		{
			if (!String.IsNullOrEmpty(App.MembreCourant.NomUtilisateur))
			{
				if (presenteurContenu.Content is FenetreVotes)
					((FenetreVotes)presenteurContenu.Content).Rafraichir();

				if (App.MembreCourant.EstBanni)
				{
					App.MembreCourant = new Membre();
					toolBarNotif.Items.Clear();
					DessinerNotificationNvnvVersion();
					DessinerNotificationNvPlat();
					ServiceFactory.Instance.GetService<IApplicationService>().ChangerVue(new MenuPrincipal());
				}
				else
				{
					if (presenteurContenu.Content is MenuPrincipalConnecte)
						ServiceFactory.Instance.GetService<IApplicationService>().ChangerVue(new MenuPrincipalConnecte());
				}

				if (App.MembreCourant.EstAdministrateur)
				{
					if (presenteurContenu.Content is MenuPrincipalConnecte)
						ServiceFactory.Instance.GetService<IApplicationService>().ChangerVue(new MenuPrincipalConnecte());
				}
				else
				{
					if (presenteurContenu.Content is Bannissement
					|| presenteurContenu.Content is FenetreGestionAdmin
					|| presenteurContenu.Content is GestionRepertoire
					|| presenteurContenu.Content is FenetreMenuAdministrateur)
						ServiceFactory.Instance.GetService<IApplicationService>().ChangerVue(new MenuPrincipalConnecte());
				}

			}

		}

		// -------------- Notifications ------------------

		/// <summary>
		/// Méthode qui regarde le nombre de bouton dans la barre d'outil des notifs et qui indique ce nombre à côté de l'icone de notif
		/// </summary>
		private void MettreAJourNbrNotif()
		{
			string txtNbrNotif = (toolBarNotif.Items.Count).ToString(); // Moins l'icone de notification et le num de notif

			if (txtNbrNotif == "0")
				txtNbrNotif = "";
			nbrNotif.Text = txtNbrNotif;
		}

		/// <summary>
		/// Crée un bouton qui se place dans la barre d'outil des notifs
		/// </summary>
		private void DessinerNotificationNvnvVersion()
		{

			if (MessageNouvelleVersion != "")
			{
				Button btnNouvelleVersion = new Button();
				Label lblNvPlat = new Label();
				lblNvPlat.Content = "Nouvelle version disponible";
				btnNouvelleVersion.Content = lblNvPlat;
				btnNouvelleVersion.Click += btnNouvelleVersion_Click;
				toolBarNotif.Items.Add(btnNouvelleVersion);

				MettreAJourNbrNotif();
			}

		}

		/// <summary>
		/// Crée un bouton qui se place dans la barre d'outil des notifs et indique le nombre de nouveaux plats
		/// </summary>
		private void DessinerNotificationNvPlat()
		{
			// On le déclare accéssible a toute la fonction car on s'en sert dans les deux contextes
			Button btnNouveauxPlats = new Button();
			if (!String.IsNullOrEmpty(App.MembreCourant.NomUtilisateur))
			{
				if (NvPlatAffichable.Count > 0)
				{
					foreach (var contenu in toolBarNotif.Items)
						if (contenu is Button)
							if (((Button)contenu).Name == "notifNvPlat")
								btnNouveauxPlats = (Button)contenu;

					btnNouveauxPlats.Name = "notifNvPlat";

					if (btnNouveauxPlats.Content is Label)
					{
						((Label)btnNouveauxPlats.Content).Content = "Nouveaux plats disponibles (" + NvPlatAffichable.Count.ToString() + ")";
					}
					else
					{
						Label lblNvPlat = new Label();
						lblNvPlat.Content = "Nouveaux plats disponibles (";
						lblNvPlat.Content += NvPlatAffichable.Count.ToString() + ")";
						btnNouveauxPlats.Content = lblNvPlat;
						btnNouveauxPlats.Click += btnNouveauxPlats_Click;
						toolBarNotif.Items.Add(btnNouveauxPlats);
					}
					MettreAJourNbrNotif();
				}

			}
			else
			{
				if (NvPlatAffichable.Count > 0)
				{
					NvPlatAffichable.Clear();
					foreach (Button notif in toolBarNotif.Items)
						if (notif.Name == "notifNvPlat")
							btnNouveauxPlats = notif;

					toolBarNotif.Items.Remove(btnNouveauxPlats);
					MettreAJourNbrNotif();
					previousTime = DateTime.MinValue;
				}
			}


		}

		/// <summary>
		/// Formate les nouveaux plats pour les affichers lors du clique sur le bouton de notif
		/// </summary>
		private void CreerMessageNouveauxPlats()
		{
			StringBuilder sbNotifs = new StringBuilder();
			sbNotifs.AppendLine("     Nouveaux plats").AppendLine("-----------------------------------------").AppendLine();
			foreach (var plat in NvPlatAffichable)
			{
				sbNotifs.Append("+ ").AppendLine(plat.Nom).AppendLine();
			}

			sbNotifs.AppendLine("-----------------------------------------").AppendLine("Voulez-vous ouvrir ces ").AppendLine("plats avec la calculatrice ?");

			if (MessageBox.Show(
					 sbNotifs.ToString(),
					 "Nouveaux plats disponibles",
					 MessageBoxButton.YesNo,
					 MessageBoxImage.Information) == MessageBoxResult.Yes)
			{
				// Si l'utilisateur à répondu oui pour afficher les plats dans la calculatrice ...<
				Window windowCalculatrice = new Window();
				windowCalculatrice.Width = App.APP_WIDTH;
				windowCalculatrice.Height = App.APP_HEIGHT;
				windowCalculatrice.ResizeMode = ResizeMode.CanMinimize;
				windowCalculatrice.Title = "Nutritia - Calculatrice nutritionnelle";
				windowCalculatrice.Icon = new BitmapImage(new Uri("pack://application:,,,/UI/Images/logoIconPetit.png"));
				Grid grdContenu = new Grid();
				ImageBrush brush = new ImageBrush();
				brush.Opacity = 0.3;
				brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/UI/Images/background.jpg"));
				grdContenu.Background = brush;
				grdContenu.Children.Add(new FenetreCalculatriceNutritionelle(NvPlatAffichable));
				windowCalculatrice.Content = grdContenu;
				windowCalculatrice.Show();
			}

		}


		// ------------------------ ÉVENEMENTS --------------------------

		/// <summary>
		/// Détecte un raccourcis
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F1)
			{
				FenetreAide fenetreAide = new FenetreAide(presenteurContenu.Content.GetType().Name);
				fenetreAide.Show();
			}
		}

		// ----------- Notification NvPlat / NvVersion -------------

		/// <summary>
		/// Configurer le message de nouvelle version.
		/// Puis donne l'option à l'utilisateur de télécharger la nouvelle version.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnNouvelleVersion_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show(
					 MessageNouvelleVersion + "\n Voulez-vous la télécharger ?",
					 "Nouvelle version disponible",
					 MessageBoxButton.YesNo,
					 MessageBoxImage.Information) == MessageBoxResult.Yes)
			{
				Process p = new Process();
				p.StartInfo.FileName = latestVersionLogiciel.DownloadLink + "/download/v" + latestVersionLogiciel.Version + "/Nutritia.zip";
				p.Start();
			}

			toolBarNotif.Items.Remove((Button)sender);

			MettreAJourNbrNotif();
		}

		/// <summary>
		/// Lorsque l'utilisateur clique sur le bouton de nouveaux plats dans la bar de notif
		/// -> On affiche tous les nouveaux plats en appellant la méthode "CreerMessageNouveauxPlats"
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnNouveauxPlats_Click(object sender, RoutedEventArgs e)
		{
			ServiceFactory.Instance.GetService<IMembreService>().Update(App.MembreCourant);

			CreerMessageNouveauxPlats();
			toolBarNotif.Items.Remove((Button)sender);
			MettreAJourNbrNotif();
		}

		// ---------------------- ToolBar --------------------------

		/// <summary>
		/// Permet, suivant le contexte, de revenir au menu précédent
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnRetour_Click(object sender, RoutedEventArgs e)
		{
			App.Current.MainWindow.ResizeMode = ResizeMode.CanMinimize;
			App.Current.MainWindow.Width = App.APP_WIDTH;
			App.Current.MainWindow.Height = App.APP_HEIGHT;
			App.Current.MainWindow.WindowState = WindowState.Normal;

			if (presenteurContenu.Content is FenetreListeEpicerie)
				ServiceFactory.Instance.GetService<IApplicationService>().ChangerVue(new FenetreGenerateurMenus(FenetreListeEpicerie.MenuGenere, FenetreListeEpicerie.NbColonnesMenu));
			else
			{
				if (String.IsNullOrEmpty(App.MembreCourant.NomUtilisateur))
					ServiceFactory.Instance.GetService<IApplicationService>().ChangerVue(new MenuPrincipal());
				else
				{
					if (presenteurContenu.Content is Bannissement || presenteurContenu.Content is FenetreGestionAdmin || presenteurContenu.Content is GestionRepertoire || presenteurContenu.Content is FenetreDons)
						ServiceFactory.Instance.GetService<IApplicationService>().ChangerVue(new FenetreMenuAdministrateur());
					else
						ServiceFactory.Instance.GetService<IApplicationService>().ChangerVue(new MenuPrincipalConnecte());
				}
			}
		}

		/// <summary>
		/// Ouvre la fenêtre des paramètres en modal
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnParam_Click(object sender, RoutedEventArgs e)
		{
			(new FenetreParametres()).ShowDialog();
		}

		/// <summary>
		/// Ouvre la fenêtre d'aide de l'application en modeless
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnAide_Click(object sender, RoutedEventArgs e)
		{
			FenetreAide fenetreAide = new FenetreAide(presenteurContenu.Content.GetType().Name);
			fenetreAide.Show();
		}

		/// <summary>
		/// Ouvre la fenêtre des infos sur l'application en modal
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnAPropos_Click(object sender, RoutedEventArgs e)
		{
			(new FenetreAPropos()).ShowDialog();
			//ServiceFactory.Instance.GetService<IApplicationService>().ChangerVue(new FenetreAPropos());
		}

		/// <summary>
		/// Lorsque l'application de ferme, il faut fermer le Thread de notif.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Closing(object sender, CancelEventArgs e)
		{
			ThreadVerifChangement.Abort();
		}



	}
}
