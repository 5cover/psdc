/** @file
 * @brief StructureExamples
 * @author raphael
 * @date 08/31/2024
 */
#include <stdbool.h>
#include <string.h>

typedef struct {
    char c_prenom[50];
    char c_nom[50];
    bool c_estUnHomme;
    int c_age;
} t_personne;
typedef struct {
    float x;
    float y
} tPoint;

int main() {
    tPoint a = {
        .x = 3.14,
        .y = 14.3,
    };
    t_personne p;
    strcpy(p.c_prenom, "Scover");
    strcpy(p.c_nom, "NoLastName");
    p.c_estUnHomme = true;
    p.c_age = 300;
    t_personne p2 = {
        "Scover",
        "NoLastName",
    };

    return 0;
}
