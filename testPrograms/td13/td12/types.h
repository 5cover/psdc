/** @file
 * @brief déclaration des types utilisateur
 * @author rbardini
 * @date 19/12/2023
*/

#ifndef TYPES_H_
#define TYPES_H_

#include <stdlib.h>

#include "const.h"
#include "date/fonctions_date.h"

typedef char t_message[MAX_CAR];

typedef struct {
    t_message message;
    char date[MAX_SIZE];
} t_element;

typedef struct {
    t_element tabElt[MAX_MESSAGES];
    size_t nb;
} t_file;

#endif // TYPES_H_