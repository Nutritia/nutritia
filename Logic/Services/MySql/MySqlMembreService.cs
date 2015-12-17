﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Windows;

namespace Nutritia
{
    /// <summary>
    /// Service MySql lié aux Membres.
    /// </summary>
    public class MySqlMembreService : IMembreService
    {
        private readonly string stringConnexion;
        private readonly IRestrictionAlimentaireService restrictionAlimentaireService;
        private readonly IObjectifService objectifService;
        private readonly IPreferenceService preferenceService;
        private readonly IMenuService menuService;

        /// <summary>
        /// Constructeur par défaut de la classe.
        /// </summary>
        public MySqlMembreService()
            : this(String.Empty)
        {

        }

        public MySqlMembreService(String connexion)
        {
            stringConnexion = connexion;
            restrictionAlimentaireService = new MySqlRestrictionAlimentaireService();
            objectifService = new MySqlObjectifService();
            preferenceService = new MySqlPreferenceService();
            menuService = new MySqlMenuService();
        }

        #region ServicesMembres

        /// <summary>
        /// Méthode permettant d'obtenir l'ensemble des membres sauvegardés dans la base de données.
        /// </summary>
        /// <returns>Une liste contenant les membres.</returns>
        public IList<Membre> RetrieveAll()
        {

            IList<Membre> resultat = new List<Membre>();

            try
            {
                //Si stringConnexion est null ou vide, constructeur vide, sinon on utilise le stringConnexion spécifié.
                using (MySqlConnexion connexion = (String.IsNullOrWhiteSpace(stringConnexion)) ? new MySqlConnexion() : new MySqlConnexion(stringConnexion))
                {
                    string requete = "SELECT * FROM Membres";

                    using (DataSet dataSetMembres = connexion.Query(requete))
                    using (DataTable tableMembres = dataSetMembres.Tables[0])
                    {
                        // Construction de chaque objet Membre.
                        foreach (DataRow rowMembre in tableMembres.Rows)
                        {
                            Membre membre = ConstruireMembre(rowMembre);

                            membre.LangueMembre = LangueFromId((int)rowMembre["idLangue"]);

                            // Ajout des restrictions alimentaires du membre.
                            requete = string.Format("SELECT idRestrictionAlimentaire FROM RestrictionsAlimentairesMembres WHERE idMembre = {0}", membre.IdMembre);

                            using (DataSet dataSetRestrictions = connexion.Query(requete))
                            using (DataTable tableRestrictions = dataSetRestrictions.Tables[0])
                            {
                                foreach (DataRow rowRestriction in tableRestrictions.Rows)
                                {
                                    membre.ListeRestrictions.Add(restrictionAlimentaireService.Retrieve(new RetrieveRestrictionAlimentaireArgs { IdRestrictionAlimentaire = (int)rowRestriction["idRestrictionAlimentaire"] }));
                                }

                                // Ajout des objectifs du membre.
                                requete = string.Format("SELECT idObjectif FROM ObjectifsMembres WHERE idMembre = {0}", membre.IdMembre);

                                using (DataSet dataSetObjectifs = connexion.Query(requete))
                                using (DataTable tableObjectifs = dataSetObjectifs.Tables[0])
                                {
                                    foreach (DataRow rowObjectif in tableObjectifs.Rows)
                                    {
                                        membre.ListeObjectifs.Add(objectifService.Retrieve(new RetrieveObjectifArgs { IdObjectif = (int)rowObjectif["idObjectif"] }));
                                    }

                                    // Ajout des préférences du membre.
                                    requete = string.Format("SELECT idPreference FROM PreferencesMembres WHERE idMembre = {0}", membre.IdMembre);

                                    using (DataSet dataSetPreferences = connexion.Query(requete))
                                    using (DataTable tablePreferences = dataSetPreferences.Tables[0])
                                    {
                                        foreach (DataRow rowPreference in tablePreferences.Rows)
                                        {
                                            membre.ListePreferences.Add(preferenceService.Retrieve(new RetrievePreferenceArgs { IdPreference = (int)rowPreference["idPreference"] }));
                                        }

                                        membre.ListeMenus = menuService.RetrieveSome(new RetrieveMenuArgs { IdMembre = (int)membre.IdMembre });

                                        resultat.Add(membre);
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (MySqlException)
            {
                throw;
            }

            return resultat;
        }

        /// <summary>
        /// Méthode permettant d'obtenir un membre sauvegardé dans la base de données.
        /// </summary>
        /// <param name="args">Les arguments permettant de retrouver le membre.</param>
        /// <returns>Un objet Membre.</returns>
        public Membre Retrieve(RetrieveMembreArgs args)
        {

            Membre membre = new Membre();

            try
            {
                //Si stringConnexion est null ou vide, constructeur vide, sinon on utilise le stringConnexion spécifié.
                using (MySqlConnexion connexion = (String.IsNullOrWhiteSpace(stringConnexion)) ? new MySqlConnexion() : new MySqlConnexion(stringConnexion))
                {

                    string requete = string.Format("SELECT * FROM Membres WHERE idMembre = {0}", args.IdMembre);

                    if (args.NomUtilisateur != null && args.NomUtilisateur != string.Empty)
                    {
                        requete = string.Format("SELECT * FROM Membres WHERE nomUtilisateur = '{0}'", args.NomUtilisateur);
                    }

                    using (DataSet dataSetMembres = connexion.Query(requete))
                    using (DataTable tableMembres = dataSetMembres.Tables[0])
                    {
                        // Construction de l'objet Membre.
                        if (tableMembres.Rows.Count != 0)
                        {
                            membre = ConstruireMembre(tableMembres.Rows[0]);

                            membre.LangueMembre = LangueFromId((int)tableMembres.Rows[0]["idLangue"]);

                            // Ajout des restrictions alimentaires du membre.
                            requete = string.Format("SELECT idRestrictionAlimentaire FROM RestrictionsAlimentairesMembres WHERE idMembre = {0}", membre.IdMembre);

                            using (DataSet dataSetRestrictions = connexion.Query(requete))
                            using (DataTable tableRestrictions = dataSetRestrictions.Tables[0])
                            {



                                foreach (DataRow rowRestriction in tableRestrictions.Rows)
                                {
                                    membre.ListeRestrictions.Add(restrictionAlimentaireService.Retrieve(new RetrieveRestrictionAlimentaireArgs { IdRestrictionAlimentaire = (int)rowRestriction["idRestrictionAlimentaire"] }));
                                }

                                // Ajout des objectifs du membre.
                                requete = string.Format("SELECT idObjectif FROM ObjectifsMembres WHERE idMembre = {0}", membre.IdMembre);

                                using (DataSet dataSetObjectifs = connexion.Query(requete))
                                using (DataTable tableObjectifs = dataSetObjectifs.Tables[0])
                                {
                                    foreach (DataRow rowObjectif in tableObjectifs.Rows)
                                    {
                                        membre.ListeObjectifs.Add(objectifService.Retrieve(new RetrieveObjectifArgs { IdObjectif = (int)rowObjectif["idObjectif"] }));
                                    }

                                    // Ajout des préférences du membre.
                                    requete = string.Format("SELECT idPreference FROM PreferencesMembres WHERE idMembre = {0}", membre.IdMembre);

                                    using (DataSet dataSetPreferences = connexion.Query(requete))
                                    using (DataTable tablePreferences = dataSetPreferences.Tables[0])
                                    {
                                        foreach (DataRow rowPreference in tablePreferences.Rows)
                                        {
                                            membre.ListePreferences.Add(preferenceService.Retrieve(new RetrievePreferenceArgs { IdPreference = (int)rowPreference["idPreference"] }));
                                        }

                                        membre.ListeMenus = menuService.RetrieveSome(new RetrieveMenuArgs { IdMembre = (int)membre.IdMembre });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException)
            {
                throw;
            }

            return membre;
        }

        /// <summary>
        /// Méthode permettant d'insérer un membre dans la base de données.
        /// </summary>
        /// <param name="membre">L'objet Membre a insérer.</param>
        public void Insert(Membre membre)
        {
            try
            {
                //Si stringConnexion est null ou vide, constructeur vide, sinon on utilise le stringConnexion spécifié.
                using (MySqlConnexion connexion = (String.IsNullOrWhiteSpace(stringConnexion)) ? new MySqlConnexion() : new MySqlConnexion(stringConnexion))
                {
                    string requete = string.Format("INSERT INTO Membres (nom ,prenom, taille, masse, dateNaissance, nomUtilisateur, motPasse, idLangue) VALUES ('{0}', '{1}', {2}, {3}, '{4}', '{5}', '{6}', (SELECT idLangue FROM Langues WHERE IETF = '{7}'))", membre.Nom, membre.Prenom, membre.Taille, membre.Masse, membre.DateNaissance.ToString("yyyy-MM-dd"), membre.NomUtilisateur, membre.MotPasse, membre.LangueMembre.IETF);
                    connexion.Query(requete);

                    int idMembre = (int)Retrieve(new RetrieveMembreArgs { NomUtilisateur = membre.NomUtilisateur }).IdMembre;

                    // Ajout des restrictions alimentaires du membre.
                    foreach (RestrictionAlimentaire restriction in membre.ListeRestrictions)
                    {
                        requete = string.Format("INSERT INTO RestrictionsAlimentairesMembres (idRestrictionAlimentaire, idMembre) VALUES ({0}, {1})", restriction.IdRestrictionAlimentaire, idMembre);
                        connexion.Query(requete);
                    }

                    // Ajout des objectifs du membre.
                    foreach (Objectif objectif in membre.ListeObjectifs)
                    {
                        requete = string.Format("INSERT INTO ObjectifsMembres (idObjectif, idMembre) VALUES ({0}, {1})", objectif.IdObjectif, idMembre);
                        connexion.Query(requete);
                    }

                    // Ajout des préférences du membre.
                    foreach (Preference preference in membre.ListePreferences)
                    {
                        requete = string.Format("INSERT INTO PreferencesMembres (idPreference, idMembre) VALUES ({0}, {1})", preference.IdPreference, idMembre);
                        connexion.Query(requete);
                    }
                }
            }
            catch (MySqlException)
            {
                throw;
            }
        }


        /// <summary>
        /// Méthode permettant de mettre à jour un membre dans la base de données.
        /// </summary>
        /// <param name="membre">L'objet Membre à mettre à jour.</param>
        public void Update(Membre membre)
        {
            try
            {
                //Si stringConnexion est null ou vide, constructeur vide, sinon on utilise le stringConnexion spécifié.
                using (MySqlConnexion connexion = (String.IsNullOrWhiteSpace(stringConnexion)) ? new MySqlConnexion() : new MySqlConnexion(stringConnexion))
                {
                    string requete = string.Format("UPDATE Membres SET nom = '{0}' ,prenom = '{1}', taille = {2}, masse = {3}, dateNaissance = '{4}', nomUtilisateur = '{5}', motPasse = '{6}', estAdmin = {7}, estBanni = {8}, idLangue = (SELECT idLangue FROM Langues WHERE IETF = '{9}'), derniereMaj = CURRENT_TIMESTAMP WHERE idMembre = {10}", membre.Nom, membre.Prenom, membre.Taille, membre.Masse, membre.DateNaissance.ToString("yyyy-MM-dd"), membre.NomUtilisateur, membre.MotPasse, membre.EstAdministrateur, membre.EstBanni, membre.LangueMembre.IETF, membre.IdMembre);

                    connexion.Query(requete);

                    string requeteEffacerRestrictions = string.Format("DELETE FROM RestrictionsAlimentairesMembres WHERE idMembre = {0}", membre.IdMembre);
                    string requeteEffacerObjectifs = string.Format("DELETE FROM ObjectifsMembres WHERE idMembre = {0}", membre.IdMembre);
                    string requeteEffacerPreferences = string.Format("DELETE FROM PreferencesMembres WHERE idMembre = {0}", membre.IdMembre);

                    connexion.Query(requeteEffacerRestrictions);
                    connexion.Query(requeteEffacerObjectifs);
                    connexion.Query(requeteEffacerPreferences);

                    // Ajout des restrictions alimentaires du membre.
                    foreach (RestrictionAlimentaire restriction in membre.ListeRestrictions)
                    {
                        requete = string.Format("INSERT INTO RestrictionsAlimentairesMembres (idRestrictionAlimentaire, idMembre) VALUES ({0}, {1})", restriction.IdRestrictionAlimentaire, membre.IdMembre);
                        connexion.Query(requete);
                    }

                    // Ajout des objectifs du membre.
                    foreach (Objectif objectif in membre.ListeObjectifs)
                    {
                        requete = string.Format("INSERT INTO ObjectifsMembres (idObjectif, idMembre) VALUES ({0}, {1})", objectif.IdObjectif, membre.IdMembre);
                        connexion.Query(requete);
                    }

                    // Ajout des préférences du membre.
                    foreach (Preference preference in membre.ListePreferences)
                    {
                        requete = string.Format("INSERT INTO PreferencesMembres (idPreference, idMembre) VALUES ({0}, {1})", preference.IdPreference, membre.IdMembre);
                        connexion.Query(requete);
                    }
                }
            }
            catch (MySqlException)
            {
                throw;
            }
        }

        /// <summary>
        /// Méthode permettant de construire un objet Membre.
        /// </summary>
        /// <param name="membre">Un enregistrement de la table Membres.</param>
        /// <returns>Un objet Membre.</returns>
        private Membre ConstruireMembre(DataRow membre)
        {
            return new Membre()
            {
                IdMembre = (int)membre["idMembre"],
                Nom = (string)membre["nom"],
                Prenom = (string)membre["prenom"],
                Taille = (double)membre["taille"],
                Masse = (double)membre["masse"],
                DateNaissance = (DateTime)membre["dateNaissance"],
                NomUtilisateur = (string)membre["nomUtilisateur"],
                MotPasse = (string)membre["motPasse"],
                ListeRestrictions = new List<RestrictionAlimentaire>(),
                ListeObjectifs = new List<Objectif>(),
                ListePreferences = new List<Preference>(),
                ListeMenus = new List<Menu>(),
                EstAdministrateur = (bool)membre["estAdmin"],
                EstBanni = (bool)membre["estBanni"],
                DerniereMaj = (DateTime)membre["derniereMaj"]

            };

        }

        /// <summary>
        /// Méthode permettant d'obtenir la liste de tous les membres avec les droits d'administration.
        /// </summary>
        /// <returns>Une liste d'objet Membre</returns>
        public IList<Membre> RetrieveAdmins()
        {
            IList<Membre> resultat = new List<Membre>();

            try
            {
                //Si stringConnexion est null ou vide, constructeur vide, sinon on utilise le stringConnexion spécifié.
                using (MySqlConnexion connexion = (String.IsNullOrWhiteSpace(stringConnexion)) ? new MySqlConnexion() : new MySqlConnexion(stringConnexion))
                {

                    string requete = "SELECT * FROM Membres WHERE estAdmin = True";

                    using (DataSet dataSetMembres = connexion.Query(requete))
                    using (DataTable tableMembres = dataSetMembres.Tables[0])
                    {
                        // Construction de chaque objet Membre.
                        foreach (DataRow rowMembre in tableMembres.Rows)
                        {
                            Membre membre = ConstruireMembre(rowMembre);

                            membre.LangueMembre = LangueFromId((int)rowMembre["idLangue"]);

                            // Ajout des restrictions alimentaires du membre.
                            requete = string.Format("SELECT idRestrictionAlimentaire FROM RestrictionsAlimentairesMembres WHERE idMembre = {0}", membre.IdMembre);

                            using (DataSet dataSetRestrictions = connexion.Query(requete))
                            using (DataTable tableRestrictions = dataSetRestrictions.Tables[0])
                            {
                                foreach (DataRow rowRestriction in tableRestrictions.Rows)
                                {
                                    membre.ListeRestrictions.Add(restrictionAlimentaireService.Retrieve(new RetrieveRestrictionAlimentaireArgs { IdRestrictionAlimentaire = (int)rowRestriction["idRestrictionAlimentaire"] }));
                                }

                                // Ajout des objectifs du membre.
                                requete = string.Format("SELECT idObjectif FROM ObjectifsMembres WHERE idMembre = {0}", membre.IdMembre);

                                using (DataSet dataSetObjectifs = connexion.Query(requete))
                                using (DataTable tableObjectifs = dataSetObjectifs.Tables[0])
                                {
                                    foreach (DataRow rowObjectif in tableObjectifs.Rows)
                                    {
                                        membre.ListeObjectifs.Add(objectifService.Retrieve(new RetrieveObjectifArgs { IdObjectif = (int)rowObjectif["idObjectif"] }));
                                    }

                                    // Ajout des préférences du membre.
                                    requete = string.Format("SELECT idPreference FROM PreferencesMembres WHERE idMembre = {0}", membre.IdMembre);

                                    using (DataSet dataSetPreferences = connexion.Query(requete))
                                    using (DataTable tablePreferences = dataSetPreferences.Tables[0])
                                    {
                                        foreach (DataRow rowPreference in tablePreferences.Rows)
                                        {
                                            membre.ListePreferences.Add(preferenceService.Retrieve(new RetrievePreferenceArgs { IdPreference = (int)rowPreference["idPreference"] }));
                                        }

                                        membre.ListeMenus = menuService.RetrieveSome(new RetrieveMenuArgs { IdMembre = (int)membre.IdMembre });

                                        resultat.Add(membre);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException)
            {
                throw;
            }

            return resultat;
        }

        /// <summary>
        /// Méthode permettant d'obtenir la date et l'heure de la dernière modification d'un membre dans la base de données.
        /// Utilisé pour voir si un temps de dernière modification devient plus récent pour remettre à jour des données pouvant être affiché.
        /// </summary>
        /// <returns>Un objet DateTime.</returns>
        public DateTime LastUpdatedTime()
        {
            DateTime time;
            try
            {
                //Si stringConnexion est null ou vide, constructeur vide, sinon on utilise le stringConnexion spécifié.
                using (MySqlConnexion connexion = (String.IsNullOrWhiteSpace(stringConnexion)) ? new MySqlConnexion() : new MySqlConnexion(stringConnexion))
                {

                    string requete = "SELECT * FROM LastModifiedMember";
                    using (DataSet dataSetLastUpdatedTime = connexion.Query(requete))
                    using (DataTable tableLastUpdatedTime = dataSetLastUpdatedTime.Tables[0])
                    {
                        time = (DateTime)tableLastUpdatedTime.Rows[0]["derniereMaj"];
                    }
                }
            }

            catch (MySqlException)
            {
                throw;
            }
            return time;
        }


        
        /// <summary>
        /// Méthode permettant d'obtenir l'objet Langue à partir du idLangue associé au Membre.
        /// </summary>
        /// <param name="id">idLangue que nous cherchons.</param>
        /// <returns>Un objet Langue.</returns>
        private Langue LangueFromId(int id)
        {
            Langue langue;
            try
            {
                //Si stringConnexion est null ou vide, constructeur vide, sinon on utilise le stringConnexion spécifié.
                using (MySqlConnexion connexion = (String.IsNullOrWhiteSpace(stringConnexion)) ? new MySqlConnexion() : new MySqlConnexion(stringConnexion))
                {

                    string requete = string.Format("SELECT IETF FROM Langues WHERE idLangue = {0}", id);
                    using (DataSet dataSetLangue = connexion.Query(requete))
                    using (DataTable tableLangue = dataSetLangue.Tables[0])
                    {
                        //Utilise la méthode LangueFromIETF de la classe Langue pour obtenir l'objet Langue à partir d'un string du tag IETF ("fr-CA").
                        langue = Langue.LangueFromIETF(tableLangue.Rows[0]["IETF"].ToString());
                    }
                }
            }
            catch (MySqlException)
            {
                throw;
            }
            return langue;
        }

        #endregion

    }
}