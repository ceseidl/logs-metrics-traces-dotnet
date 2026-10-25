# Security Policy / Política de Segurança

[English](#english) | [Português](#português)

## English

`logs-metrics-traces-dotnet` is **educational code** that accompanies an article. It is not meant to run in production, and only the latest commit on `main` is maintained.

### Reporting a vulnerability

Please **do not open a public issue** for a security problem.

Use GitHub's private channel instead: go to the **Security** tab of this repository and choose **Report a vulnerability** (<https://github.com/ceseidl/logs-metrics-traces-dotnet/security/advisories/new>). Only the maintainer can see the report.

Please include what you found, how to reproduce it and, if possible, a suggested fix. This is a personal project, so the response is best effort: I will try to acknowledge the report within 7 days.

### What is expected, not a vulnerability

These are deliberate simplifications of a didactic example (the README lists the main ones):

- The three services listen on 127.0.0.1 without authentication, and the delays are simulated. The signals are collected in memory with `ActivityListener` and `MeterListener` and printed to the console; a real system would use OpenTelemetry and a telemetry backend.

The project has no NuGet packages; it only uses the .NET 10 shared framework.

## Português

O `logs-metrics-traces-dotnet` é **código didático** que acompanha um artigo. Não foi feito para rodar em produção, e só o último commit da `main` é mantido.

### Como reportar uma vulnerabilidade

Por favor, **não abra uma issue pública** para um problema de segurança.

Use o canal privado do GitHub: na aba **Security** deste repositório, escolha **Report a vulnerability** (<https://github.com/ceseidl/logs-metrics-traces-dotnet/security/advisories/new>). Só o mantenedor vê o relato.

Informe o que encontrou, como reproduzir e, se possível, uma sugestão de correção. É um projeto pessoal, então a resposta é feita na medida do possível: vou tentar confirmar o recebimento em até 7 dias.

### O que é esperado e não é vulnerabilidade

São simplificações deliberadas de um exemplo didático (o README lista as principais):

- Os três serviços escutam em 127.0.0.1 sem autenticação, e os atrasos são simulados. Os sinais são coletados em memória com `ActivityListener` e `MeterListener` e impressos no console; um sistema real usaria o OpenTelemetry e um backend de telemetria.

O projeto não tem pacotes NuGet; usa só o framework compartilhado do .NET 10.
