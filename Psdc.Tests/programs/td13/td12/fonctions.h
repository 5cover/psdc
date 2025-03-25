/** @file
 * @brief déclaration des prototypes des fonctions
 * @author rbardini
 * @date 19/12/2023
*/

#ifndef FONCTIONS_H_
#define FONCTIONS_H_

#include <stdbool.h>

#include "types.h"

t_file initialiser(void);
bool enfiler(t_file *file, t_element nouvelElt);
t_element defiler(t_file *file);
void vider(t_file *file);
t_element tete(t_file const *file);
bool estVide(t_file const *file);
bool estPleine(t_file const *file);

void afficherElement(t_element e);
t_element saisieElement(void);

void supprimer_trop_anciens(t_file *file, int nbASupprimer);
void sauvegardeFichier(t_file const *file, char const *nomFichier);
void lectureFichier(t_file *file, char const *nomFichier);

// secret.. chuuut
void afficherTous(t_file file);

void saisirNomFichier(char const *mode, t_message nomFichier);

void supprimer_anciens_date(t_file *file, time_t unixSecondsDateMin);

#endif // FONCTIONS_H_