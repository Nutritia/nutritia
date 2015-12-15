﻿using Nutritia.Logic.Model.Entities;
using Nutritia.Toolkit;
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
using System.Windows.Shapes;

namespace Nutritia.UI.Views
{
    /// <summary>
    /// Fenêtre pour afficher un recu pour un don
    /// </summary>
    public partial class FenetreRecuDon : Window
    {
        public FenetreRecuDon(Don transaction)
        {
            InitializeComponent();
            lblDate.Content = transaction.DateHeureTransaction.ToString("dd/MM/yy");
            lblHeure.Content = transaction.DateHeureTransaction.ToString("HH:mm");
            lblModePaiement.Content = transaction.ModePaiementTransaction.ToString();
            lblMontant.Content = transaction.Montant.ToString() + "$";
            lblNom.Content = transaction.NomAuteur;
            //lblNoTransaction.Content += " " + transaction.NoTransaction.ToString();
            imgQrCode.Source = QrCodeHelper.getQrBitmap(transaction.ToString());
        }
    }
}
