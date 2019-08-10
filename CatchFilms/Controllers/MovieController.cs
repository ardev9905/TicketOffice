﻿using CatchFilms.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CatchFilms.Controllers
{
    public class MovieController : Controller
    {
        public async Task<ActionResult> Billboard()
        {
            List<Movie> Movies = new List<Movie>();
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(LoginController.BaseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (Session["userAutentication"] != null)
                {                    
                    client.DefaultRequestHeaders.Authorization = new 
                        AuthenticationHeaderValue("Bearer", Session["userAutentication"].ToString());
                }

                HttpResponseMessage res = await client.GetAsync("api/movies");

                if (res.IsSuccessStatusCode)
                {
                    var response = res.Content.ReadAsStringAsync().Result;
                    Debug.WriteLine(response);
                    Movies = JsonConvert.DeserializeObject<List<Movie>>(response);

                }
                else if (res.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("unauthorized", "error");
                }
                Debug.WriteLine("Codigo de respuesta: "+res.StatusCode);
                return View(Movies);
            }
        }

        public async Task<ActionResult> Details(int? id)
        {
            dynamic models = new ExpandoObject();
            HttpStatusCode statusCode = HttpStatusCode.Unauthorized;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(LoginController.BaseUrl);
                if (Session["userAutentication"] != null)
                {
                    client.DefaultRequestHeaders.Authorization = new
                        AuthenticationHeaderValue("Bearer", Session["userAutentication"].ToString());
                }
                var responseTask = client.GetAsync(String.Concat("api/movies/", id));
                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsAsync<Movie>();
                    readTask.Wait();
                    models.movie = readTask.Result;
                    List<Function> functions = await new FunctionController().List(statusCode, Session["userAutentication"].ToString());
                    models.functions = functions;
                    models.moviePremier = functions.First().time.ToString("dddd, MMMM dd, yyyy", new CultureInfo("es-ES"));
                }
            }

            return View(models);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Movie movie)
        {
            movie.movieID = null;
            Debug.WriteLine(JsonConvert.SerializeObject(movie));

            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(LoginController.BaseUrl);

                    if (Session["userAutentication"] != null)
                    {
                        client.DefaultRequestHeaders.Authorization = new
                            AuthenticationHeaderValue("Bearer", Session["userAutentication"].ToString());
                    }
                    var postTask = client.PostAsJsonAsync<Movie>("api/movies", movie);

                    postTask.Wait();
                    var res = postTask.Result;

                    if (res.IsSuccessStatusCode)
                    {
                        return RedirectToAction("create", "movie");
                    }
                    else if (res.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("unauthorized", "error");
                    }

                    Debug.WriteLine("ERROR: " + res.StatusCode);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("ERROR: " + e.Message);
                }

                ModelState.AddModelError(String.Empty, "Ocurrió un error al tratar de crear una nueva pelicula");
                return RedirectToAction("create", "movie");
            }
        }
    }
}