typedef char t[];


int main() {
    int a = 0;
    struct { int a; } s1 = { a };
    s1[5];
}

void func(t val)
{
    
}