int main() {
    int a[5];
    int i;

    for (i = 0; i <= 5; ++i) {
        a[i] = 0;
        // &a[5] == &i
    }
}