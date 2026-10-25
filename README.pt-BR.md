[English](README.md) | Português

# Logs, métricas e traces em .NET

Código do artigo "Logs, métricas e traces em .NET: três visões do mesmo incidente".

Três serviços ASP.NET Core (`gateway` → `orders` → `payments`) rodam num só processo. No meio da execução, o `payments` passa da versão 1.4.0 para a 1.5.0, que tem uma verificação antifraude lenta. O programa segue o incidente pelos três sinais:

- **Métricas**: `http.server.request.duration`, com nome e versão do serviço como tags.
- **Logs**: cada linha guarda o trace id da requisição.
- **Traces**: a árvore de spans da requisição que falhou, passando pelos três serviços.

Sem pacotes NuGet: os sinais são lidos com `ActivityListener` e `MeterListener`. Em produção, use o OpenTelemetry.

## Requisitos

- SDK do .NET 10

## Como rodar

```bash
dotnet run --project src/Signals
```

## Saída esperada (as latências variam um pouco)

```text
metrics: http.server.request.duration
service   version  requests  5xx    p50     p95
gateway   2.1.0         400   10     46 ms  208 ms
orders    3.0.2         400   10     45 ms  108 ms
payments  1.4.0         200    0     30 ms   43 ms
payments  1.5.0         200   10     31 ms  807 ms

logs: 400 lines, 10 errors
  error payments "Fraud check timed out for order 220"
        trace=b6cc653596e1ddbc4eebb2f2e6493937
  ...

trace b6cc653596e1ddbc4eebb2f2e6493937
  load: call /checkout/220                    825 ms  ERROR
    gateway: POST /checkout/{id:int}          824 ms  ERROR
      ...
              payments: fraud-check           812 ms  ERROR
```

## Estrutura

- `Program.cs`: sobe os serviços, manda 200 checkouts antes e 200 depois do deploy e imprime o relatório.
- `Services.cs`: os três serviços, o middleware que marca nome e versão e o span da verificação antifraude.
- `Telemetry.cs`: coleta em memória de spans, métricas e logs.
- `Report.cs`: tabela de métricas, logs de erro e árvore de spans.
