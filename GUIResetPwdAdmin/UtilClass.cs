using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GUIResetPwdAdmin
{
    class UtilClass
    {

   
        public volatile static ObservableCollection<TableComputers> TableComputersList=new ObservableCollection<TableComputers>();


        /// <summary>
        /// Возвращает найденные обьекты из АД согласно фильтру
        /// </summary>
        /// <param name="ou">Место поиска</param>
        /// <param name="Filter">Параметры фильтра</param>
        /// <returns>Возвращает SearchResultCollection</returns>
        public static SearchResultCollection LDAPFindAll(string ou, string Filter)
        {
            if (!string.IsNullOrEmpty(ou))
            {
                try
                {
                    string sDomain;

                    using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                    {
                        sDomain = context.ConnectedServer;

                    }

                    var domainPath = @"LDAP://" + sDomain + ":636/" + ou;
                    var directoryEntry = new DirectoryEntry(domainPath);
                    var dirSearcher = new DirectorySearcher(directoryEntry);
                    dirSearcher.SearchScope = SearchScope.Subtree;
                    dirSearcher.PageSize = 100;
                    dirSearcher.SizeLimit = 5000;
                    dirSearcher.Filter = Filter;

                    return dirSearcher.FindAll();
                }
                catch { return null; }
            }
            else return null;
        }

        /// <summary>
        /// Возвращает найденные обьекты из АД согласно фильтру
        /// </summary>
        /// <param name="ou">Место поиска</param>
        /// <param name="obj">имя объекта</param>
        /// <param name="ldf">LdapFilter:  выбор обьекта</param>
        /// <returns>Возвращает SearchResultCollection</returns>
        public static SearchResultCollection LDAPFindAll(string ou, string obj, LdapFilter ldf)
        {
            string filter = "";


            switch (ldf)
            {
                case LdapFilter.Computers: filter = "(&(objectCategory=computer)(name=" + obj + "))"; break;
                case LdapFilter.OU: filter = "(objectCategory=organizationalUnit)"; break;//----------!!!!!!!!
                case LdapFilter.UsersSN: filter = "(&(objectCategory=person)(objectClass=user)(sAMAccountName=" + obj + "))"; break;
                case LdapFilter.UsersCN: filter = "(&(objectCategory=person)(objectClass=user)(CN=" + obj + "*))"; break;
                case LdapFilter.UsersName: filter = "(&(objectCategory=person)(objectClass=user)(name=" + obj + "*))"; break;
                case LdapFilter.Groups: { filter = obj != "" ? "(&(objectCategory=group)(name=" + obj + ")) " : "(&(objectCategory=group)(name=*))"; } break;
            }

            return LDAPFindAll(ou, filter);
        }

        public async static Task GetInfoComputers(string hostAdress)
        {

            TableComputers data = new TableComputers
                {
                    Name = hostAdress,
                    Status = "Получение локальных записей",
                    BStatus = true

                };

            await Task.Run(() =>
            {

                Users usr = GetUsersComputers(hostAdress);

                if (usr != null)
                {
                    data.Admin = usr.LocalAdm.Name;
                    data.Guest = usr.Ghost.Name;

                    if (usr.isEnableLocalAdm) data.AdmEnable = "Включен";
                    else data.AdmEnable = "Отключен";

                    if (usr.isEnableGhost) data.GstEnable = "Включен";
                    else data.GstEnable = "Отключен";

                    foreach (var nAdm in usr.OtherAdmins)
                    {
                       
                            data.OtherAdmins += nAdm.Name+";";

                    }


                    foreach (var nUser in usr.OtherUsers)
                    {

                        data.OtherUsers += nUser.Item1.Name + "("+ nUser.Item2 + ");";

                    }

                    data.Status = "OK";
                }
                else
                {
                    data.Admin = "Error";
                    data.Guest = "Error";
                    data.AdmEnable = "Error";
                    data.GstEnable = "Error";
                    data.Status = "Error. Получение локальных записей";
                    data.BStatus = false;
                }

                SetDataGridData(data);
            });

        }

        public static void SetInfoComputers(ControlForm ctrl)
        {
            List<Task> tsk = new List<Task>();

                foreach (var comp in TableComputersList)
                {

                    if (!comp.PingFail || ctrl.IngnorPing)
                    {
                        tsk.Add(SetInfoComputersAsync(comp.Name, ctrl));
                    }

                }

            Task.WaitAll(tsk.ToArray());
             
        }

        private async static Task SetInfoComputersAsync(string hostAdress, ControlForm ctrl)
        {
            TableComputers data = new TableComputers
            {
                Name = hostAdress,
                Status = "Установка локальных записей",
                BStatus = true

            };

            int ADS_UF_ACCOUNTDISABLE = 2;

              await Task.Run(() =>
              {
                  do
                  {

                      try
                      {

                          Users usr = GetUsersComputers(hostAdress);

                          if (usr == null) { data.Status = "Error. user null"; data.BStatus = false; SetDataGridData(data); return; }
                          //-------------Администратор--------------------------

                          //включение / отключение администратора
                          try
                          {

                              int control = (int)usr.LocalAdm.Properties["UserFlags"].Value;

                              if (ctrl.EnableLocalAdm)
                              {
                                  usr.LocalAdm.Properties["UserFlags"].Value = (control & ~ADS_UF_ACCOUNTDISABLE);
                                  data.AdmEnable = "Включен";
                              }
                              else
                              {
                                  usr.LocalAdm.Properties["UserFlags"].Value = (control | ADS_UF_ACCOUNTDISABLE);
                                  data.AdmEnable = "Отключен";
                              }

                              usr.LocalAdm.CommitChanges();

                          }
                          catch
                          {
                              Console.WriteLine("Включение/Отключение localAdm" + hostAdress);

                              data.BStatus = false;
                              data.AdmEnable = "Error";
                              data.Admin = "Error. Включение/Отключение";
                          }

                          //переименование администратора
                          if (ctrl.remLocalAdm)
                          {
                              if (!string.IsNullOrEmpty(ctrl.nameLocalAdm))
                              {
                                  try
                                  {
                                      usr.LocalAdm.Rename(ctrl.nameLocalAdm);
                                      usr.LocalAdm.CommitChanges();
                                      data.Admin = ctrl.nameLocalAdm;
                                  }
                                  catch
                                  {

                                      data.BStatus = false;
                                      data.Admin = "Error. переименование";
                                  }
                              }
                              else
                              {
                                  data.BStatus = false;
                                  data.Admin = "Error. Не задано имя";
                              }
                          }

                          //смена пароля администратора
                          if (ctrl.setPwdLocalAdm)
                          {
                              if (!string.IsNullOrEmpty(ctrl.password))
                              {
                                  try
                                  {

                                      usr.LocalAdm.Invoke("SetPassword", new object[] { ctrl.password });
                                      usr.LocalAdm.CommitChanges();
                                      data.AdmPass = "OK";

                                  }
                                  catch
                                  {
                                      data.BStatus = false;
                                      data.AdmPass = "Error. Установка пароля";
                                  }
                              }
                              else
                              {

                                  data.BStatus = false;
                                  data.AdmPass = "Error. Не задан пароль";

                              }

                          }


                          ////////////////////////////////////////////////////////////////////

                          //-----------------Гость--------------------------

                          //отклбчение гостя
                          if (ctrl.DisableGHost)
                          {
                              try
                              {
                                  int control = (int)usr.Ghost.Properties["UserFlags"].Value;
                                  usr.Ghost.Properties["UserFlags"].Value = (control | ADS_UF_ACCOUNTDISABLE);
                                  data.GstEnable = "Отключен";
                                  usr.Ghost.CommitChanges();
                              }
                              catch
                              {
                                  Console.WriteLine("Ошибка отключения ghost" + hostAdress);

                                  data.BStatus = false;
                                  data.Guest = "Error. Отключение";
                                  data.GstEnable = "Error";
                              }
                          }

                          //переименование гостя
                          if (ctrl.remGHost)
                          {
                              if (!string.IsNullOrEmpty(ctrl.nameGHost))
                              {
                                  try
                                  {
                                      usr.Ghost.Rename(ctrl.nameGHost);
                                      usr.Ghost.CommitChanges();
                                      data.Guest = ctrl.nameGHost;
                                  }
                                  catch
                                  {

                                      data.BStatus = false;
                                      data.Guest = "Error. переименование";
                                  }
                              }
                              else
                              {
                                  data.BStatus = false;
                                  data.Guest = "Error. Не задано имя";
                              }
                          }


                          ////////////////////////////////////////////////////////////////////

                          //Удаление иных УЗ
                          if (ctrl.RemoveUS)
                          {
                              if (usr.OtherUsers.Count()>0)
                              {
                                  try
                                  {
                                     
                                      data.OtherUsers = "Удален: ";
                                      foreach (var duser in usr.OtherUsers)
                                      {
                                          if (Convert.ToInt32(duser.Item2) > 1000)
                                          {
                                              var temp_usr = duser.Item1.Name;
                                              duser.Item1.Parent.Children.Remove(duser.Item1);
                                              data.OtherUsers += temp_usr + ";";
                                          }
                                             
                                      }
  
                                  }
                                  catch
                                  {

                                      data.BStatus = false;
                                      data.OtherUsers = "Error. Удаление";
                                  }
                              }
                              else
                              {
                                  data.BStatus = false;
                                  data.OtherUsers = "Нечего удалять";
                              }
                          }



                      }
                      catch
                      {
                          data.BStatus = false;
                          data.Status = "Error";
                      }

                      if (data.BStatus)
                      {
                          data.Status = "OK";

                      }
                      else { data.Status = "Error"; }

                      SetDataGridData(data);

                      if (ctrl.WaitReady)
                      {
                          if (data.BStatus)
                          {
                              break;
                          }

                          Task.Delay(TimeSpan.FromMinutes(3)).Wait();
                      }


                  } while (ctrl.WaitReady);


            });
        }

        private static Users GetUsersComputers(string hostAdress)
        {
            Console.WriteLine("Получение локальных записей "+ hostAdress);

            Users usr = new Users();
            usr.OtherAdmins = new List<DirectoryEntry>();
            usr.OtherUsers = new List<Tuple<DirectoryEntry, string>>();

            try
                {
                    DirectoryEntry AD = new DirectoryEntry("WinNT://" + hostAdress + ",computer");

                    foreach (DirectoryEntry child in AD.Children)
                    {

                   
                    if (child.SchemaClassName == "User")
                    {
                        var sid = new SecurityIdentifier((byte[])child.Properties["objectSid"].Value, 0);

                        Console.WriteLine("Name: {0}", child.Name);
                        Console.WriteLine("SID: {0}", sid);

                        var ind = sid.ToString().Split('-');

                        if (ind[7] == "500")
                        {
                            Console.WriteLine("Встроенный админ");
                            usr.LocalAdm = child;

                        }
                        else

                        if (ind[7] == "501")
                        {
                            Console.WriteLine("Встроенный Гость");
                            usr.Ghost = child;

                        }
                        else
                        {
                            usr.OtherUsers.Add(new Tuple<DirectoryEntry, string>(child, ind[7]));


                        }

                        Console.WriteLine("-----------------------------------");
                    }
                    else
                    {
                        if (child.SchemaClassName == "Group")
                        {
                            var sid = new SecurityIdentifier((byte[])child.Properties["objectSid"].Value, 0);

                            Console.WriteLine("Name: {0}", child.Name);
                            Console.WriteLine("SID: {0}", sid);

                            //if (child.Name == "Администраторы")
                            if(sid.Value == "S-1-5-32-544")
                            {
                                try
                                {
                                    foreach (object member in (IEnumerable)child.Invoke("Members"))
                                    {
                                        DirectoryEntry memberEntry = new DirectoryEntry(member);

                                        usr.OtherAdmins.Add(memberEntry);
                                        Console.WriteLine(memberEntry.Path);

                                    }

                                }
                                catch
                                {

                                }
                                break;

                            }
                                     
                        }
                            
                    }

                    }

                if (usr.LocalAdm != null)
                {
                    int flags = (int)usr.LocalAdm.Properties["UserFlags"].Value;
                    usr.isEnableLocalAdm = (!Convert.ToBoolean(flags & 0x0002));

                    flags = (int)usr.Ghost.Properties["UserFlags"].Value;
                    usr.isEnableGhost = (!Convert.ToBoolean(flags & 0x0002));
                }
                else usr.Error = "Error.";

                }
                catch (Exception err)
                {
                usr.Error = err.Message;
                }


            if (string.IsNullOrEmpty(usr.Error))
            {

                return usr;

            }
            else
            {

                return null;
            }
 

        }


        public static void PingComputers()
        {
            List<Task> tsk = new List<Task>();
             foreach (var comp in TableComputersList)
              {
               tsk.Add(PingerAsync(comp.Name));
              }

            Task.WaitAll(tsk.ToArray());

        }

        private async static Task PingerAsync(string hostAdress)
        {
            TableComputers data = new TableComputers
            {
                Name = hostAdress,
                Status= "Проверка Ping",
                BStatus=false,
                PingFail =true
            };

            Thread.Sleep(100);

            try
            {
                Ping png = new Ping();
                var rez =await png.SendPingAsync(hostAdress);
                if (rez.Status == IPStatus.Success)
                {
                    data.Ping = rez.Address.ToString();
                    data.BStatus = true;
                    data.PingFail = false;

                }
                else {

                    data.Ping = "Error. "+ rez.Status.ToString();
                    data.Status = "Error";
                }

                Console.WriteLine("Status for {0} = {1}, ip-адрес: {2}", hostAdress, rez.Status, rez.Address);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникла ошибка! " + hostAdress);
                data.Ping = "Error. Исключение Ping";
                data.Status = "Error";
            }   

        if (!data.PingFail)
            {
                GetInfoComputers(hostAdress);

            }

         //   data.PingStop = true;

            SetDataGridData(data);                    

        }

        private static void SetDataGridData(TableComputers data)
        {
            for (int i = 0; i < TableComputersList.Count; i++)
            {
                if (TableComputersList[i].Name == data.Name)
                {
                    if (!string.IsNullOrEmpty(data.Description)) TableComputersList[i].Description = data.Description;
                    if (!string.IsNullOrEmpty(data.Ping)) TableComputersList[i].Ping = data.Ping;
                    if (!string.IsNullOrEmpty(data.Admin)) TableComputersList[i].Admin = data.Admin;
                    if (!string.IsNullOrEmpty(data.AdmEnable)) TableComputersList[i].AdmEnable = data.AdmEnable;
                    if (!string.IsNullOrEmpty(data.Guest)) TableComputersList[i].Guest = data.Guest;
                    if (!string.IsNullOrEmpty(data.GstEnable)) TableComputersList[i].GstEnable = data.GstEnable;
                    if (!string.IsNullOrEmpty(data.AdmPass)) TableComputersList[i].AdmPass = data.AdmPass;
                    if (!string.IsNullOrEmpty(data.Status)) TableComputersList[i].Status = data.Status;
                    if (!string.IsNullOrEmpty(data.OtherAdmins)) TableComputersList[i].OtherAdmins = data.OtherAdmins;
                    if (!string.IsNullOrEmpty(data.OtherUsers)) TableComputersList[i].OtherUsers = data.OtherUsers;
                    TableComputersList[i].BStatus = data.BStatus;
                    TableComputersList[i].PingFail = data.PingFail;
                  //  TableComputersList[i].PingStop = data.PingStop;
                  //  TableComputersList[i].DoStop = data.DoStop;

                    break;
                }

            }
        }

        public static void LDAPFindComputers(string PathOU)
        {
            string filter = "(objectCategory=computer)";
           

            SearchResultCollection objSearch = UtilClass.LDAPFindAll(PathOU, filter);

            if (objSearch != null)
                if (objSearch.Count > 0)
                    foreach (SearchResult comp in objSearch)
                    {
                        bool enable = false;

                        string name = comp.Properties["name"][0].ToString();
                        int Control = (int)comp.Properties["useraccountcontrol"][0];

                        string description = "";
                        if (comp.Properties.Contains("description"))
                        {
                            description = comp.Properties["description"][0].ToString();
                        }


                        if ((Control & 0x2) > 0) { enable = false; } else { enable = true; };

                        string distinguishedName = comp.Properties["distinguishedName"][0].ToString();


                        if (distinguishedName.IndexOf("Disabled Accounts") == -1)
                        {
                            if (enable)
                            {
                                TableComputersList.Add(

                                    new TableComputers
                                    {
                                        Name = name,
                                        Description = description,
                                        Ping = "не определен",
                                        Status = "не определен",
                                        Admin = "не определен",
                                        AdmPass = "не определен",
                                        Guest = "не определен",
                                        AdmEnable= "не определен",
                                        GstEnable = "не определен",
                                        BStatus = false,
                                        OtherAdmins= "",
                                        OtherUsers= "",
                                        PingFail=false


                                    }


                                    );


                            }

                        }



                    }

        }
    }
}
