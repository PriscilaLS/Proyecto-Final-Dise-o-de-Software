from datetime import datetime


def calcular_promedio(notas):
    return sum(notas) / len(notas)


def evaluar_estado(promedio):
    if promedio >= 70:
        return "Aprobado"

    return "Requiere mejora"


notas = [88, 92, 79, 95]
promedio = calcular_promedio(notas)
estado = evaluar_estado(promedio)

print("Demo EduIDE - ejecucion Python")
print("Fecha de prueba:", datetime.now().strftime("%Y-%m-%d %H:%M"))
print("Notas:", notas)
print("Promedio:", round(promedio, 2))
print("Estado:", estado)
