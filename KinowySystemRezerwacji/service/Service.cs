﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinowySystemRezerwacji.dto;
using KinowySystemRezerwacji.service.dao;
using KinowySystemRezerwacji.service.model;

namespace KinowySystemRezerwacji.service
{
    /// <summary>
    /// Logika biznesowa aplikacji.
    /// </summary>
    internal class Service
    {
        #region Private fields

        /// <summary>
        /// Obiekt zalogowanego użytkownika.
        /// </summary>
        private UzytkownikEntity loggedUser = null;

        #endregion

        #region Shared with Presenter

        /// <summary>
        /// Event potwierdzający ukończenie logowania się do systemu.
        /// </summary>
        internal event Action<string> UpdateLoggedInAs;

        /// <summary>
        /// Event reprezentujący prostą odpowiedź informującą o suckesie danej operacji.
        /// </summary>
        internal event Action<bool, string> BasicResponse;

        /// <summary>
        /// Metoda rejestrująca w bazie dnaych nowego użytkownika.
        /// </summary>
        /// <param name="request">Dane dotyczące nowego użytkownika</param>
        internal void Register(RegisterRequest request)
        {
            UzytkownikRepository uzytkownikRepository = new UzytkownikRepository();
            if (uzytkownikRepository.ExistsByNazwaUzytkownika(request.Username))
            {
                BasicResponse?.Invoke(false, "Użytkownik o takiej nazwie już istnieje");
            }
            else
            {
                UzytkownikEntity userToRegister = new UzytkownikEntity(request.Username, request.Password, request.FirstName, request.LastName, request.Email, request.Birthday);
                uzytkownikRepository.Save(userToRegister);
                BasicResponse?.Invoke(true, "Pomyślnie zarejestrowano użytkownika");
            }
        }

        /// <summary>
        /// Metoda logująca użytkownika.
        /// </summary>
        /// <param name="username">Nazwa użytkownika</param>
        /// <param name="password">Hasło użytkownika</param>
        internal void LogIn(string username, string password)
        {
            string NOT_LOGGED_IN = "Nie udało się zalogować";
            UzytkownikRepository uzytkownikRepository = new UzytkownikRepository();
            UzytkownikEntity uzytkownik = uzytkownikRepository.FindByNazwaUzytkownika(username).OrElseThrow(NOT_LOGGED_IN);
            if (Security.HashPassword(password) == uzytkownik.Haslo)
            {
                loggedUser = uzytkownik;
                UpdateLoggedInAs?.Invoke(uzytkownik.NazwaUzytkownika);
            }
            else
            {
                throw new Exception(NOT_LOGGED_IN);
            }
        }

        /// <summary>
        /// Metoda wylogowywująca użytkownika.
        /// </summary>
        internal void LogOut()
        {
            loggedUser = null;
            UpdateLoggedInAs?.Invoke(null);
        }

        /// <summary>
        /// Metoda zwracająca listę wcześniejszych rezerwacji.
        /// </summary>
        /// <returns>Informacje o wcześniejszych rezerwacjach</returns>
        internal BookingResponse[] GetBookings()
        {
            if (loggedUser != null)
            {
                //TODO: to jest mock
                return new BookingResponse[]
                {
                    new BookingResponse()
                    {
                        FilmName = "Shrek",
                        DateTime = new DateTime(2019, 06, 30, 12, 0, 0),
                        Seats = new BookedSeatResponse[]
                        {
                            new BookedSeatResponse()
                            {
                                PosX = 4,
                                PosY = 6
                            },
                            new BookedSeatResponse()
                            {
                                PosX = 4,
                                PosY = 7
                            }
                        }
                    },
                    new BookingResponse()
                    {
                        FilmName = "Miss marca",
                        DateTime = new DateTime(2019, 07, 02, 17, 0, 0),
                        Seats = new BookedSeatResponse[]
                        {
                            new BookedSeatResponse()
                            {
                                PosX = 6,
                                PosY = 10
                            },
                            new BookedSeatResponse()
                            {
                                PosX = 7,
                                PosY = 10
                            },
                            new BookedSeatResponse()
                            {
                                PosX = 8,
                                PosY = 10
                            }
                        }
                    },
                    new BookingResponse()
                    {
                        FilmName = "Moonlight",
                        DateTime = new DateTime(2019, 07, 04, 12, 0, 0),
                        Seats = new BookedSeatResponse[]
                        {
                            new BookedSeatResponse()
                            {
                                PosX = 6,
                                PosY = 9
                            }
                        }
                    }
                };
            }
            else
            {
                throw new Exception("Nie uzyskano autoryzacji do wykonania zadania");
            }
        }

        /// <summary>
        /// Metoda zwracająca listę dostępnych seansów.
        /// </summary>
        /// <param name="date">Data, dla której mają zostać wyświetlone seanse</param>
        /// <returns>Informacje o dostępnych seansach</returns>
        internal ShowingResponse[] GetShowings(DateTime date)
        {
            SeansRepository projectionsRepo = new SeansRepository();
            FilmRepository moviesRepo = new FilmRepository();
            List<SeansEntity> projections = projectionsRepo.FindByKiedy(date);
            List<ShowingResponse> responses = new List<ShowingResponse>();
            FilmEntity tempFilmEntity;

            foreach (var projection in projections)
            {
                tempFilmEntity = moviesRepo.FindById(projection.IdFilmu);
                responses.Add(new ShowingResponse
                {
                    Id = projection.Id,
                    DateTime = projection.Kiedy,
                    FilmName = tempFilmEntity.Nazwa,
                    FilmDuration = tempFilmEntity.CzasTrwania,
                    FilmYear = tempFilmEntity.RokPremiery
                });
            }

            return responses.ToArray();
        }

        /// <summary>
        /// Metoda zwracająca listę miejsc na dany seans.
        /// </summary>
        /// <param name="showingId">ID wybranego seansu</param>
        /// <returns>Informacje o miejscach na sali</returns>
        internal SeatToChooseResponse[] GetSeats(int showingId)
        {
            MiejsceRepository miejsceRepository = new MiejsceRepository();
            MiejsceRezerwacjaRepository miejsceRezerwacjaRepository = new MiejsceRezerwacjaRepository();
            RezerwacjaRepository rezerwacjaRepository = new RezerwacjaRepository();

            List<MiejsceEntity> zajeteMiejsca = new List<MiejsceEntity>();
            List<RezerwacjaEntity> rezerwacje = rezerwacjaRepository.FindAllBySeansId(showingId);
            foreach (RezerwacjaEntity rezerwacja in rezerwacje)
            {
                List<MiejsceRezerwacjaEntity> miejscaRezerwacje = miejsceRezerwacjaRepository.FindAllByRezerwacjaId(rezerwacja.Id);
                foreach (MiejsceRezerwacjaEntity miejsceRezerwacja in miejscaRezerwacje)
                {
                    zajeteMiejsca.Add(miejsceRepository.FindById(miejsceRezerwacja.IdMiejsca).OrElseThrow("Nie istnieje miejsce o podanym ID"));
                }
            }

            List<MiejsceEntity> wszystkieMiejsca = miejsceRepository.FindAll();
            List<SeatToChooseResponse> seats = new List<SeatToChooseResponse>();
            foreach (MiejsceEntity miejsce in wszystkieMiejsca)
            {
                SeatToChooseResponse seat = new SeatToChooseResponse()
                {
                    Id = miejsce.Id,
                    PosX = miejsce.Numer,
                    PosY = miejsce.Rzad,
                    Available = true
                };

                foreach (MiejsceEntity zajeteMiejsce in zajeteMiejsca)
                {
                    if (miejsce.Id == zajeteMiejsce.Id)
                    {
                        seat.Available = false;
                        break;
                    }
                }

                seats.Add(seat);
            }

            return seats.ToArray();
        }

        /// <summary>
        /// Metoda tworząca rezerwację dla zalogowanego użytkownika.
        /// </summary>
        /// <param name="request">Dane dotyczące wybranego seansu i miejsc</param>
        internal void BookSeats(BookSeatsRequest request)
        {
            if (loggedUser != null)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new Exception("Nie uzyskano autoryzacji do wykonania zadania");
            }
        }

        #endregion
    }
}
