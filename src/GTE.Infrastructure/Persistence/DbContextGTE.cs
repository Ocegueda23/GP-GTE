using System;
using System.Collections.Generic;
using GTE.Infrastructure.Modelos.bdsGTE;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Persistence;

public partial class DbContextGTE : DbContext
{
    public DbContextGTE(DbContextOptions<DbContextGTE> options)
        : base(options)
    {
    }

    public virtual DbSet<TblAmbiente> TblAmbiente { get; set; }

    public virtual DbSet<TblAprobacion> TblAprobacion { get; set; }

    public virtual DbSet<TblArchivo> TblArchivo { get; set; }

    public virtual DbSet<TblArchivoVinculo> TblArchivoVinculo { get; set; }

    public virtual DbSet<TblArea> TblArea { get; set; }

    public virtual DbSet<TblArtefacto> TblArtefacto { get; set; }

    public virtual DbSet<TblArticuloConocimiento> TblArticuloConocimiento { get; set; }

    public virtual DbSet<TblArticuloVersion> TblArticuloVersion { get; set; }

    public virtual DbSet<TblAusencia> TblAusencia { get; set; }

    public virtual DbSet<TblBitacora> TblBitacora { get; set; }

    public virtual DbSet<TblBitacoraCambio> TblBitacoraCambio { get; set; }

    public virtual DbSet<TblCapacidadSprint> TblCapacidadSprint { get; set; }

    public virtual DbSet<TblCasoPrueba> TblCasoPrueba { get; set; }

    public virtual DbSet<TblCasoPruebaPaso> TblCasoPruebaPaso { get; set; }

    public virtual DbSet<TblCategoriaProyecto> TblCategoriaProyecto { get; set; }

    public virtual DbSet<TblCategoriaTicket> TblCategoriaTicket { get; set; }

    public virtual DbSet<TblCicloPrueba> TblCicloPrueba { get; set; }

    public virtual DbSet<TblComentario> TblComentario { get; set; }

    public virtual DbSet<TblCommit> TblCommit { get; set; }

    public virtual DbSet<TblCommitWorkItem> TblCommitWorkItem { get; set; }

    public virtual DbSet<TblComplejidad> TblComplejidad { get; set; }

    public virtual DbSet<TblDespliegue> TblDespliegue { get; set; }

    public virtual DbSet<TblDiaFestivo> TblDiaFestivo { get; set; }

    public virtual DbSet<TblEjecucionPrueba> TblEjecucionPrueba { get; set; }

    public virtual DbSet<TblEncuestaSatisfaccion> TblEncuestaSatisfaccion { get; set; }

    public virtual DbSet<TblEquipo> TblEquipo { get; set; }

    public virtual DbSet<TblEquipoMiembro> TblEquipoMiembro { get; set; }

    public virtual DbSet<TblEstatusAprobacion> TblEstatusAprobacion { get; set; }

    public virtual DbSet<TblEstatusAusencia> TblEstatusAusencia { get; set; }

    public virtual DbSet<TblEstatusDespliegue> TblEstatusDespliegue { get; set; }

    public virtual DbSet<TblEstatusIncidente> TblEstatusIncidente { get; set; }

    public virtual DbSet<TblEstatusProyecto> TblEstatusProyecto { get; set; }

    public virtual DbSet<TblEstatusRelease> TblEstatusRelease { get; set; }

    public virtual DbSet<TblEstatusRevision> TblEstatusRevision { get; set; }

    public virtual DbSet<TblEstatusRiesgo> TblEstatusRiesgo { get; set; }

    public virtual DbSet<TblEstatusSolicitud> TblEstatusSolicitud { get; set; }

    public virtual DbSet<TblEstatusSprint> TblEstatusSprint { get; set; }

    public virtual DbSet<TblEstatusTicket> TblEstatusTicket { get; set; }

    public virtual DbSet<TblEstatusWorkItem> TblEstatusWorkItem { get; set; }

    public virtual DbSet<TblEtiqueta> TblEtiqueta { get; set; }

    public virtual DbSet<TblEventoDominio> TblEventoDominio { get; set; }

    public virtual DbSet<TblFolio> TblFolio { get; set; }

    public virtual DbSet<TblHistorialCampo> TblHistorialCampo { get; set; }

    public virtual DbSet<TblHistorialEstatus> TblHistorialEstatus { get; set; }

    public virtual DbSet<TblHito> TblHito { get; set; }

    public virtual DbSet<TblHorario> TblHorario { get; set; }

    public virtual DbSet<TblHorarioTramo> TblHorarioTramo { get; set; }

    public virtual DbSet<TblIncidente> TblIncidente { get; set; }

    public virtual DbSet<TblKpiDefinicion> TblKpiDefinicion { get; set; }

    public virtual DbSet<TblKpiValor> TblKpiValor { get; set; }

    public virtual DbSet<TblLocacion> TblLocacion { get; set; }

    public virtual DbSet<TblMatrizPresupuesto> TblMatrizPresupuesto { get; set; }

    public virtual DbSet<TblNivel> TblNivel { get; set; }

    public virtual DbSet<TblNotificacion> TblNotificacion { get; set; }

    public virtual DbSet<TblObjetivoOkr> TblObjetivoOkr { get; set; }

    public virtual DbSet<TblPermiso> TblPermiso { get; set; }

    public virtual DbSet<TblPipelineEjecucion> TblPipelineEjecucion { get; set; }

    public virtual DbSet<TblPlanPrueba> TblPlanPrueba { get; set; }

    public virtual DbSet<TblPlantillaNotificacion> TblPlantillaNotificacion { get; set; }

    public virtual DbSet<TblPortafolio> TblPortafolio { get; set; }

    public virtual DbSet<TblPresupuestoProyecto> TblPresupuestoProyecto { get; set; }

    public virtual DbSet<TblPrioridad> TblPrioridad { get; set; }

    public virtual DbSet<TblProceso> TblProceso { get; set; }

    public virtual DbSet<TblPrograma> TblPrograma { get; set; }

    public virtual DbSet<TblProyecto> TblProyecto { get; set; }

    public virtual DbSet<TblPuesto> TblPuesto { get; set; }

    public virtual DbSet<TblPullRequest> TblPullRequest { get; set; }

    public virtual DbSet<TblRefreshToken> TblRefreshToken { get; set; }

    public virtual DbSet<TblRegistroTiempo> TblRegistroTiempo { get; set; }

    public virtual DbSet<TblReglaAutomatizacion> TblReglaAutomatizacion { get; set; }

    public virtual DbSet<TblRelease> TblRelease { get; set; }

    public virtual DbSet<TblReleaseArtefacto> TblReleaseArtefacto { get; set; }

    public virtual DbSet<TblRepositorio> TblRepositorio { get; set; }

    public virtual DbSet<TblResultadoClave> TblResultadoClave { get; set; }

    public virtual DbSet<TblResultadoPrueba> TblResultadoPrueba { get; set; }

    public virtual DbSet<TblRevision> TblRevision { get; set; }

    public virtual DbSet<TblRiesgo> TblRiesgo { get; set; }

    public virtual DbSet<TblRol> TblRol { get; set; }

    public virtual DbSet<TblRolPermiso> TblRolPermiso { get; set; }

    public virtual DbSet<TblSeveridad> TblSeveridad { get; set; }

    public virtual DbSet<TblSla> TblSla { get; set; }

    public virtual DbSet<TblSolicitud> TblSolicitud { get; set; }

    public virtual DbSet<TblSprint> TblSprint { get; set; }

    public virtual DbSet<TblTablero> TblTablero { get; set; }

    public virtual DbSet<TblTableroColumna> TblTableroColumna { get; set; }

    public virtual DbSet<TblTarifaNivel> TblTarifaNivel { get; set; }

    public virtual DbSet<TblTicket> TblTicket { get; set; }

    public virtual DbSet<TblTipoArtefacto> TblTipoArtefacto { get; set; }

    public virtual DbSet<TblTipoAusencia> TblTipoAusencia { get; set; }

    public virtual DbSet<TblTipoPrueba> TblTipoPrueba { get; set; }

    public virtual DbSet<TblTipoSolicitud> TblTipoSolicitud { get; set; }

    public virtual DbSet<TblTipoVinculo> TblTipoVinculo { get; set; }

    public virtual DbSet<TblTipoWorkItem> TblTipoWorkItem { get; set; }

    public virtual DbSet<TblTransicion> TblTransicion { get; set; }

    public virtual DbSet<TblTransicionConfig> TblTransicionConfig { get; set; }

    public virtual DbSet<TblUsuario> TblUsuario { get; set; }

    public virtual DbSet<TblUsuarioRol> TblUsuarioRol { get; set; }

    public virtual DbSet<TblUsuarioSolicitante> TblUsuarioSolicitante { get; set; }

    public virtual DbSet<TblVersionSistema> TblVersionSistema { get; set; }

    public virtual DbSet<TblWorkItem> TblWorkItem { get; set; }

    public virtual DbSet<TblWorkItemVinculo> TblWorkItemVinculo { get; set; }

    public virtual DbSet<VwBandejaTrabajo> VwBandejaTrabajo { get; set; }

    public virtual DbSet<VwTiempoInvertido> VwTiempoInvertido { get; set; }

    public virtual DbSet<VwCostoRegistroTiempo> VwCostoRegistroTiempo { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Modern_Spanish_CI_AS");

        modelBuilder.Entity<TblAmbiente>(entity =>
        {
            entity.HasKey(e => e.IdAmbiente);

            entity.ToTable("tblAmbiente");

            entity.HasIndex(e => new { e.Nombre, e.IdProyecto }, "UQ_tblAmbiente_NombreProyecto").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.BaseDatos).HasMaxLength(200);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Servidor).HasMaxLength(200);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblAmbiente)
                .HasForeignKey(d => d.IdProyecto)
                .HasConstraintName("FK_tblAmbiente_tblProyecto");

            entity.HasOne(d => d.IdResponsableNavigation).WithMany(p => p.TblAmbiente)
                .HasForeignKey(d => d.IdResponsable)
                .HasConstraintName("FK_tblAmbiente_tblUsuario");
        });

        modelBuilder.Entity<TblAprobacion>(entity =>
        {
            entity.HasKey(e => e.IdAprobacion);

            entity.ToTable("tblAprobacion");

            entity.HasIndex(e => new { e.Entidad, e.IdEntidad }, "IX_tblAprobacion_Entidad");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Comentario).HasMaxLength(500);
            entity.Property(e => e.Entidad).HasMaxLength(100);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FirmaHash).HasMaxLength(200);
            entity.Property(e => e.RolAprobacion).HasMaxLength(100);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdAprobadorNavigation).WithMany(p => p.TblAprobacion)
                .HasForeignKey(d => d.IdAprobador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblAprobacion_tblUsuario");

            entity.HasOne(d => d.IdEstatusAprobacionNavigation).WithMany(p => p.TblAprobacion)
                .HasForeignKey(d => d.IdEstatusAprobacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblAprobacion_tblEstatusAprobacion");
        });

        modelBuilder.Entity<TblArchivo>(entity =>
        {
            entity.HasKey(e => e.IdArchivo);

            entity.ToTable("tblArchivo");

            entity.HasIndex(e => e.GuidArchivo, "UQ_tblArchivo_GuidArchivo").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Extension).HasMaxLength(20);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.GuidArchivo).HasDefaultValueSql("(newid())");
            entity.Property(e => e.HashSha256).HasMaxLength(100);
            entity.Property(e => e.NombreArchivo).HasMaxLength(200);
            entity.Property(e => e.RutaRelativa).HasMaxLength(500);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblArchivoVinculo>(entity =>
        {
            entity.HasKey(e => e.IdArchivoVinculo);

            entity.ToTable("tblArchivoVinculo");

            entity.HasIndex(e => new { e.Entidad, e.IdEntidad }, "IX_tblArchivoVinculo_Entidad");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Entidad).HasMaxLength(100);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdArchivoNavigation).WithMany(p => p.TblArchivoVinculo)
                .HasForeignKey(d => d.IdArchivo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblArchivoVinculo_tblArchivo");
        });

        modelBuilder.Entity<TblArea>(entity =>
        {
            entity.HasKey(e => e.IdArea);

            entity.ToTable("tblArea");

            entity.HasIndex(e => e.Nombre, "UQ_tblArea_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblArtefacto>(entity =>
        {
            entity.HasKey(e => e.IdArtefacto);

            entity.ToTable("tblArtefacto");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.HashSha256).HasMaxLength(100);
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdArchivoNavigation).WithMany(p => p.TblArtefacto)
                .HasForeignKey(d => d.IdArchivo)
                .HasConstraintName("FK_tblArtefacto_tblArchivo");

            entity.HasOne(d => d.IdPipelineEjecucionNavigation).WithMany(p => p.TblArtefacto)
                .HasForeignKey(d => d.IdPipelineEjecucion)
                .HasConstraintName("FK_tblArtefacto_tblPipelineEjecucion");

            entity.HasOne(d => d.IdTipoArtefactoNavigation).WithMany(p => p.TblArtefacto)
                .HasForeignKey(d => d.IdTipoArtefacto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblArtefacto_tblTipoArtefacto");
        });

        modelBuilder.Entity<TblArticuloConocimiento>(entity =>
        {
            entity.HasKey(e => e.IdArticuloConocimiento);

            entity.ToTable("tblArticuloConocimiento");

            entity.HasIndex(e => e.Titulo, "UQ_tblArticuloConocimiento_Titulo").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Titulo).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
            entity.Property(e => e.VersionActual).HasDefaultValue(1);
        });

        modelBuilder.Entity<TblArticuloVersion>(entity =>
        {
            entity.HasKey(e => e.IdArticuloVersion);

            entity.ToTable("tblArticuloVersion");

            entity.HasIndex(e => new { e.IdArticuloConocimiento, e.Version }, "UQ_tblArticuloVersion_ArticuloVersion").IsUnique();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdArticuloConocimientoNavigation).WithMany(p => p.TblArticuloVersion)
                .HasForeignKey(d => d.IdArticuloConocimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblArticuloVersion_tblArticuloConocimiento");
        });

        modelBuilder.Entity<TblAusencia>(entity =>
        {
            entity.HasKey(e => e.IdAusencia);

            entity.ToTable("tblAusencia");

            entity.HasIndex(e => new { e.IdUsuario, e.FechaInicio, e.FechaFin }, "IX_tblAusencia_UsuarioFechas");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEstatusAusenciaNavigation).WithMany(p => p.TblAusencia)
                .HasForeignKey(d => d.IdEstatusAusencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblAusencia_tblEstatusAusencia");

            entity.HasOne(d => d.IdTipoAusenciaNavigation).WithMany(p => p.TblAusencia)
                .HasForeignKey(d => d.IdTipoAusencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblAusencia_tblTipoAusencia");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblAusencia)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblAusencia_tblUsuario");
        });

        modelBuilder.Entity<TblBitacora>(entity =>
        {
            entity.HasKey(e => e.IdBitacora);

            entity.ToTable("tblBitacora");

            entity.HasIndex(e => new { e.Entidad, e.IdEntidad, e.Fecha }, "IX_tblBitacora_EntidadFecha");

            entity.HasIndex(e => new { e.Usuario, e.Fecha }, "IX_tblBitacora_UsuarioFecha");

            entity.Property(e => e.Accion).HasMaxLength(100);
            entity.Property(e => e.Endpoint).HasMaxLength(500);
            entity.Property(e => e.Entidad).HasMaxLength(100);
            entity.Property(e => e.Fecha).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Ip).HasMaxLength(50);
            entity.Property(e => e.Usuario).HasMaxLength(200);
        });

        modelBuilder.Entity<TblBitacoraCambio>(entity =>
        {
            entity.HasKey(e => e.IdBitacoraCambio);

            entity.ToTable("tblBitacoraCambio");

            entity.HasIndex(e => new { e.IdAmbiente, e.Fecha }, "IX_tblBitacoraCambio_AmbienteFecha");

            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Fecha).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Usuario).HasMaxLength(200);

            entity.HasOne(d => d.IdAmbienteNavigation).WithMany(p => p.TblBitacoraCambio)
                .HasForeignKey(d => d.IdAmbiente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblBitacoraCambio_tblAmbiente");

            entity.HasOne(d => d.IdReleaseNavigation).WithMany(p => p.TblBitacoraCambio)
                .HasForeignKey(d => d.IdRelease)
                .HasConstraintName("FK_tblBitacoraCambio_tblRelease");
        });

        modelBuilder.Entity<TblCapacidadSprint>(entity =>
        {
            entity.HasKey(e => e.IdCapacidadSprint);

            entity.ToTable("tblCapacidadSprint");

            entity.HasIndex(e => new { e.IdSprint, e.IdUsuario }, "UQ_tblCapacidadSprint_SprintUsuario").IsUnique();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.HorasPorDia).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.PorcentajeDedicacion)
                .HasDefaultValue(100m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdSprintNavigation).WithMany(p => p.TblCapacidadSprint)
                .HasForeignKey(d => d.IdSprint)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblCapacidadSprint_tblSprint");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblCapacidadSprint)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblCapacidadSprint_tblUsuario");
        });

        modelBuilder.Entity<TblCasoPrueba>(entity =>
        {
            entity.HasKey(e => e.IdCasoPrueba);

            entity.ToTable("tblCasoPrueba");

            entity.HasIndex(e => e.IdWorkItem, "IX_tblCasoPrueba_WorkItem").HasFilter("([IdWorkItem] IS NOT NULL)");

            entity.HasIndex(e => e.Folio, "UQ_tblCasoPrueba_Folio")
                .IsUnique()
                .HasFilter("([Folio] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Folio).HasMaxLength(50);
            entity.Property(e => e.Titulo).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdPlanPruebaNavigation).WithMany(p => p.TblCasoPrueba)
                .HasForeignKey(d => d.IdPlanPrueba)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblCasoPrueba_tblPlanPrueba");

            entity.HasOne(d => d.IdTipoPruebaNavigation).WithMany(p => p.TblCasoPrueba)
                .HasForeignKey(d => d.IdTipoPrueba)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblCasoPrueba_tblTipoPrueba");

            entity.HasOne(d => d.IdWorkItemNavigation).WithMany(p => p.TblCasoPrueba)
                .HasForeignKey(d => d.IdWorkItem)
                .HasConstraintName("FK_tblCasoPrueba_tblWorkItem");
        });

        modelBuilder.Entity<TblCasoPruebaPaso>(entity =>
        {
            entity.HasKey(e => e.IdCasoPruebaPaso);

            entity.ToTable("tblCasoPruebaPaso");

            entity.HasIndex(e => new { e.IdCasoPrueba, e.NumeroPaso }, "UQ_tblCasoPruebaPaso_CasoNumero").IsUnique();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdCasoPruebaNavigation).WithMany(p => p.TblCasoPruebaPaso)
                .HasForeignKey(d => d.IdCasoPrueba)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblCasoPruebaPaso_tblCasoPrueba");
        });

        modelBuilder.Entity<TblCategoriaProyecto>(entity =>
        {
            entity.ToTable("tblCategoriaProyecto");

            entity.HasIndex(e => e.Nombre, "UQ_tblCategoriaProyecto_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblCategoriaTicket>(entity =>
        {
            entity.HasKey(e => e.IdCategoriaTicket);

            entity.ToTable("tblCategoriaTicket");

            entity.HasIndex(e => e.Nombre, "UQ_tblCategoriaTicket_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblCicloPrueba>(entity =>
        {
            entity.HasKey(e => e.IdCicloPrueba);

            entity.ToTable("tblCicloPrueba");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdPlanPruebaNavigation).WithMany(p => p.TblCicloPrueba)
                .HasForeignKey(d => d.IdPlanPrueba)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblCicloPrueba_tblPlanPrueba");
        });

        modelBuilder.Entity<TblComentario>(entity =>
        {
            entity.HasKey(e => e.IdComentario);

            entity.ToTable("tblComentario");

            entity.HasIndex(e => new { e.Entidad, e.IdEntidad }, "IX_tblComentario_Entidad");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Entidad).HasMaxLength(100);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdComentarioPadreNavigation).WithMany(p => p.InverseIdComentarioPadreNavigation)
                .HasForeignKey(d => d.IdComentarioPadre)
                .HasConstraintName("FK_tblComentario_tblComentario");
        });

        modelBuilder.Entity<TblCommit>(entity =>
        {
            entity.HasKey(e => e.IdCommit);

            entity.ToTable("tblCommit");

            entity.HasIndex(e => new { e.IdRepositorio, e.Sha }, "UQ_tblCommit_RepositorioSha").IsUnique();

            entity.Property(e => e.Autor).HasMaxLength(200);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Sha).HasMaxLength(64);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdRepositorioNavigation).WithMany(p => p.TblCommit)
                .HasForeignKey(d => d.IdRepositorio)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblCommit_tblRepositorio");
        });

        modelBuilder.Entity<TblCommitWorkItem>(entity =>
        {
            entity.HasKey(e => e.IdCommitWorkItem);

            entity.ToTable("tblCommitWorkItem");

            entity.HasIndex(e => new { e.IdCommit, e.IdWorkItem }, "UQ_tblCommitWorkItem_CommitWorkItem").IsUnique();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdCommitNavigation).WithMany(p => p.TblCommitWorkItem)
                .HasForeignKey(d => d.IdCommit)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblCommitWorkItem_tblCommit");

            entity.HasOne(d => d.IdWorkItemNavigation).WithMany(p => p.TblCommitWorkItem)
                .HasForeignKey(d => d.IdWorkItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblCommitWorkItem_tblWorkItem");
        });

        modelBuilder.Entity<TblComplejidad>(entity =>
        {
            entity.HasKey(e => e.IdComplejidad);

            entity.ToTable("tblComplejidad");

            entity.HasIndex(e => e.Nombre, "UQ_tblComplejidad_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdCategoriaProyectoNavigation).WithMany(p => p.TblComplejidad)
                .HasForeignKey(d => d.IdCategoriaProyecto)
                .HasConstraintName("FK_tblComplejidad_tblCategoriaProyecto");
        });

        modelBuilder.Entity<TblDespliegue>(entity =>
        {
            entity.HasKey(e => e.IdDespliegue);

            entity.ToTable("tblDespliegue");

            entity.HasIndex(e => new { e.IdAmbiente, e.FechaInicio }, "IX_tblDespliegue_Ambiente");

            entity.Property(e => e.FechaInicio).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdAmbienteNavigation).WithMany(p => p.TblDespliegue)
                .HasForeignKey(d => d.IdAmbiente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblDespliegue_tblAmbiente");

            entity.HasOne(d => d.IdEjecutorNavigation).WithMany(p => p.TblDespliegue)
                .HasForeignKey(d => d.IdEjecutor)
                .HasConstraintName("FK_tblDespliegue_tblUsuario");

            entity.HasOne(d => d.IdEstatusDespliegueNavigation).WithMany(p => p.TblDespliegue)
                .HasForeignKey(d => d.IdEstatusDespliegue)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblDespliegue_tblEstatusDespliegue");

            entity.HasOne(d => d.IdReleaseNavigation).WithMany(p => p.TblDespliegue)
                .HasForeignKey(d => d.IdRelease)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblDespliegue_tblRelease");
        });

        modelBuilder.Entity<TblDiaFestivo>(entity =>
        {
            entity.HasKey(e => e.IdDiaFestivo);

            entity.ToTable("tblDiaFestivo");

            entity.HasIndex(e => new { e.Fecha, e.IdHorario }, "UQ_tblDiaFestivo_FechaHorario").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdHorarioNavigation).WithMany(p => p.TblDiaFestivo)
                .HasForeignKey(d => d.IdHorario)
                .HasConstraintName("FK_tblDiaFestivo_tblHorario");
        });

        modelBuilder.Entity<TblEjecucionPrueba>(entity =>
        {
            entity.HasKey(e => e.IdEjecucionPrueba);

            entity.ToTable("tblEjecucionPrueba");

            entity.HasIndex(e => e.IdCicloPrueba, "IX_tblEjecucionPrueba_Ciclo");

            entity.Property(e => e.FechaEjecucion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdCasoPruebaNavigation).WithMany(p => p.TblEjecucionPrueba)
                .HasForeignKey(d => d.IdCasoPrueba)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblEjecucionPrueba_tblCasoPrueba");

            entity.HasOne(d => d.IdCicloPruebaNavigation).WithMany(p => p.TblEjecucionPrueba)
                .HasForeignKey(d => d.IdCicloPrueba)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblEjecucionPrueba_tblCicloPrueba");

            entity.HasOne(d => d.IdEjecutorNavigation).WithMany(p => p.TblEjecucionPrueba)
                .HasForeignKey(d => d.IdEjecutor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblEjecucionPrueba_tblUsuario");

            entity.HasOne(d => d.IdResultadoPruebaNavigation).WithMany(p => p.TblEjecucionPrueba)
                .HasForeignKey(d => d.IdResultadoPrueba)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblEjecucionPrueba_tblResultadoPrueba");
        });

        modelBuilder.Entity<TblEncuestaSatisfaccion>(entity =>
        {
            entity.HasKey(e => e.IdEncuestaSatisfaccion);

            entity.ToTable("tblEncuestaSatisfaccion");

            entity.HasIndex(e => e.IdTicket, "UQ_tblEncuestaSatisfaccion_Ticket").IsUnique();

            entity.Property(e => e.Comentario).HasMaxLength(500);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdTicketNavigation).WithOne(p => p.TblEncuestaSatisfaccion)
                .HasForeignKey<TblEncuestaSatisfaccion>(d => d.IdTicket)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblEncuestaSatisfaccion_tblTicket");
        });

        modelBuilder.Entity<TblEquipo>(entity =>
        {
            entity.HasKey(e => e.IdEquipo);

            entity.ToTable("tblEquipo");

            entity.HasIndex(e => e.Nombre, "UQ_tblEquipo_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdLiderNavigation).WithMany(p => p.TblEquipo)
                .HasForeignKey(d => d.IdLider)
                .HasConstraintName("FK_tblEquipo_tblUsuario");
        });

        modelBuilder.Entity<TblEquipoMiembro>(entity =>
        {
            entity.HasKey(e => e.IdEquipoMiembro);

            entity.ToTable("tblEquipoMiembro");

            entity.HasIndex(e => new { e.IdEquipo, e.IdUsuario }, "UQ_tblEquipoMiembro_EquipoUsuario").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PorcentajeDedicacion)
                .HasDefaultValue(100m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.RolEquipo).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.TblEquipoMiembro)
                .HasForeignKey(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblEquipoMiembro_tblEquipo");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblEquipoMiembro)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblEquipoMiembro_tblUsuario");
        });

        modelBuilder.Entity<TblEstatusAprobacion>(entity =>
        {
            entity.ToTable("tblEstatusAprobacion");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusAprobacion_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusAusencia>(entity =>
        {
            entity.ToTable("tblEstatusAusencia");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusAusencia_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusDespliegue>(entity =>
        {
            entity.ToTable("tblEstatusDespliegue");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusDespliegue_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusIncidente>(entity =>
        {
            entity.ToTable("tblEstatusIncidente");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusIncidente_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusProyecto>(entity =>
        {
            entity.ToTable("tblEstatusProyecto");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusProyecto_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusRelease>(entity =>
        {
            entity.ToTable("tblEstatusRelease");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusRelease_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusRevision>(entity =>
        {
            entity.ToTable("tblEstatusRevision");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusRevision_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusRiesgo>(entity =>
        {
            entity.ToTable("tblEstatusRiesgo");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusRiesgo_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusSolicitud>(entity =>
        {
            entity.ToTable("tblEstatusSolicitud");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusSolicitud_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusSprint>(entity =>
        {
            entity.ToTable("tblEstatusSprint");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusSprint_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusTicket>(entity =>
        {
            entity.ToTable("tblEstatusTicket");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusTicket_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEstatusWorkItem>(entity =>
        {
            entity.ToTable("tblEstatusWorkItem");

            entity.HasIndex(e => e.Descripcion, "UQ_tblEstatusWorkItem_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblEtiqueta>(entity =>
        {
            entity.HasKey(e => e.IdEtiqueta);

            entity.ToTable("tblEtiqueta");

            entity.HasIndex(e => e.Nombre, "UQ_tblEtiqueta_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblEventoDominio>(entity =>
        {
            entity.HasKey(e => e.IdEventoDominio);

            entity.ToTable("tblEventoDominio");

            entity.HasIndex(e => e.FechaRegistro, "IX_tblEventoDominio_Pendientes").HasFilter("([FechaProcesado] IS NULL)");

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TipoEvento).HasMaxLength(100);
        });

        modelBuilder.Entity<TblFolio>(entity =>
        {
            entity.HasKey(e => e.IdFolio);

            entity.ToTable("tblFolio");

            entity.HasIndex(e => e.Serie, "UQ_tblFolio_Serie").IsUnique();

            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Serie).HasMaxLength(50);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
        });

        modelBuilder.Entity<TblHistorialCampo>(entity =>
        {
            entity.HasKey(e => e.IdHistorialCampo);

            entity.ToTable("tblHistorialCampo");

            entity.HasIndex(e => new { e.Entidad, e.IdEntidad, e.Fecha }, "IX_tblHistorialCampo_Entidad");

            entity.Property(e => e.Campo).HasMaxLength(100);
            entity.Property(e => e.Entidad).HasMaxLength(100);
            entity.Property(e => e.Fecha).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Usuario).HasMaxLength(200);
        });

        modelBuilder.Entity<TblHistorialEstatus>(entity =>
        {
            entity.HasKey(e => e.IdHistorialEstatus);

            entity.ToTable("tblHistorialEstatus");

            entity.HasIndex(e => new { e.Proceso, e.IdEstatus }, "IX_tblHistorialEstatus_Abiertos").HasFilter("([FechaFin] IS NULL)");

            entity.HasIndex(e => new { e.Proceso, e.IdRegistro, e.FechaInicio }, "IX_tblHistorialEstatus_Registro");

            entity.Property(e => e.Accion).HasMaxLength(50);
            entity.Property(e => e.FechaInicio).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.Property(e => e.Proceso).HasMaxLength(50);
            entity.Property(e => e.Usuario).HasMaxLength(200);
        });

        modelBuilder.Entity<TblHito>(entity =>
        {
            entity.HasKey(e => e.IdHito);

            entity.ToTable("tblHito");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblHito)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblHito_tblProyecto");
        });

        modelBuilder.Entity<TblHorario>(entity =>
        {
            entity.HasKey(e => e.IdHorario);

            entity.ToTable("tblHorario");

            entity.HasIndex(e => e.Nombre, "UQ_tblHorario_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblHorarioTramo>(entity =>
        {
            entity.HasKey(e => e.IdHorarioTramo);

            entity.ToTable("tblHorarioTramo");

            entity.HasIndex(e => new { e.IdHorario, e.DiaSemana, e.HoraInicio }, "UQ_tblHorarioTramo_HorarioDiaInicio").IsUnique();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.HoraFin).HasPrecision(0);
            entity.Property(e => e.HoraInicio).HasPrecision(0);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdHorarioNavigation).WithMany(p => p.TblHorarioTramo)
                .HasForeignKey(d => d.IdHorario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblHorarioTramo_tblHorario");
        });

        modelBuilder.Entity<TblIncidente>(entity =>
        {
            entity.HasKey(e => e.IdIncidente);

            entity.ToTable("tblIncidente");

            entity.HasIndex(e => new { e.IdEstatusIncidente, e.IdSeveridad }, "IX_tblIncidente_Abiertos");

            entity.HasIndex(e => e.Folio, "UQ_tblIncidente_Folio")
                .IsUnique()
                .HasFilter("([Folio] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Folio).HasMaxLength(50);
            entity.Property(e => e.Titulo).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEstatusIncidenteNavigation).WithMany(p => p.TblIncidente)
                .HasForeignKey(d => d.IdEstatusIncidente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblIncidente_tblEstatusIncidente");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblIncidente)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblIncidente_tblProyecto");

            entity.HasOne(d => d.IdReleaseCausanteNavigation).WithMany(p => p.TblIncidente)
                .HasForeignKey(d => d.IdReleaseCausante)
                .HasConstraintName("FK_tblIncidente_tblRelease");

            entity.HasOne(d => d.IdSeveridadNavigation).WithMany(p => p.TblIncidente)
                .HasForeignKey(d => d.IdSeveridad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblIncidente_tblSeveridad");

            entity.HasOne(d => d.IdWorkItemCorrectivoNavigation).WithMany(p => p.TblIncidente)
                .HasForeignKey(d => d.IdWorkItemCorrectivo)
                .HasConstraintName("FK_tblIncidente_tblWorkItem");
        });

        modelBuilder.Entity<TblKpiDefinicion>(entity =>
        {
            entity.HasKey(e => e.IdKpiDefinicion);

            entity.ToTable("tblKpiDefinicion");

            entity.HasIndex(e => e.Clave, "UQ_tblKpiDefinicion_Clave").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Clave).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Direccion).HasMaxLength(10);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Meta).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblKpiValor>(entity =>
        {
            entity.HasKey(e => e.IdKpiValor);

            entity.ToTable("tblKpiValor");

            entity.HasIndex(e => new { e.IdKpiDefinicion, e.Fecha, e.Alcance }, "UQ_tblKpiValor_KpiFechaAlcance").IsUnique();

            entity.Property(e => e.Alcance)
                .HasMaxLength(100)
                .HasDefaultValue("global");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Valor).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.IdKpiDefinicionNavigation).WithMany(p => p.TblKpiValor)
                .HasForeignKey(d => d.IdKpiDefinicion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblKpiValor_tblKpiDefinicion");
        });

        modelBuilder.Entity<TblLocacion>(entity =>
        {
            entity.HasKey(e => e.IdLocacion);

            entity.ToTable("tblLocacion");

            entity.Property(e => e.Descripcion).HasMaxLength(150);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Locacion).HasMaxLength(50);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblMatrizPresupuesto>(entity =>
        {
            entity.HasKey(e => e.IdMatrizPresupuesto);

            entity.ToTable("tblMatrizPresupuesto");

            entity.HasIndex(e => new { e.IdComplejidad, e.IdNivel }, "UQ_tblMatrizPresupuesto_ComplejidadNivel").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Puntos).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdComplejidadNavigation).WithMany(p => p.TblMatrizPresupuesto)
                .HasForeignKey(d => d.IdComplejidad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblMatrizPresupuesto_tblComplejidad");

            entity.HasOne(d => d.IdNivelNavigation).WithMany(p => p.TblMatrizPresupuesto)
                .HasForeignKey(d => d.IdNivel)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblMatrizPresupuesto_tblNivel");
        });

        modelBuilder.Entity<TblNivel>(entity =>
        {
            entity.HasKey(e => e.IdNivel);

            entity.ToTable("tblNivel");

            entity.HasIndex(e => e.Nombre, "UQ_tblNivel_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblNotificacion>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion);

            entity.ToTable("tblNotificacion");

            entity.HasIndex(e => e.IdUsuario, "IX_tblNotificacion_NoLeidas").HasFilter("([Leida]=(0))");

            entity.Property(e => e.Entidad).HasMaxLength(100);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.Titulo).HasMaxLength(200);
            entity.Property(e => e.Url).HasMaxLength(500);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblNotificacion)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblNotificacion_tblUsuario");
        });

        modelBuilder.Entity<TblObjetivoOkr>(entity =>
        {
            entity.HasKey(e => e.IdObjetivoOkr);

            entity.ToTable("tblObjetivoOkr");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.TblObjetivoOkr)
                .HasForeignKey(d => d.IdEquipo)
                .HasConstraintName("FK_tblObjetivoOkr_tblEquipo");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblObjetivoOkr)
                .HasForeignKey(d => d.IdProyecto)
                .HasConstraintName("FK_tblObjetivoOkr_tblProyecto");
        });

        modelBuilder.Entity<TblPermiso>(entity =>
        {
            entity.HasKey(e => e.IdPermiso);

            entity.ToTable("tblPermiso");

            entity.HasIndex(e => e.Clave, "UQ_tblPermiso_Clave").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Clave).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Modulo).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblPipelineEjecucion>(entity =>
        {
            entity.HasKey(e => e.IdPipelineEjecucion);

            entity.ToTable("tblPipelineEjecucion");

            entity.HasIndex(e => new { e.IdRepositorio, e.Numero }, "UQ_tblPipelineEjecucion_RepositorioNumero").IsUnique();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Resultado).HasMaxLength(20);
            entity.Property(e => e.Tipo).HasMaxLength(20);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdAmbienteNavigation).WithMany(p => p.TblPipelineEjecucion)
                .HasForeignKey(d => d.IdAmbiente)
                .HasConstraintName("FK_tblPipelineEjecucion_tblAmbiente");

            entity.HasOne(d => d.IdRepositorioNavigation).WithMany(p => p.TblPipelineEjecucion)
                .HasForeignKey(d => d.IdRepositorio)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblPipelineEjecucion_tblRepositorio");
        });

        modelBuilder.Entity<TblPlanPrueba>(entity =>
        {
            entity.HasKey(e => e.IdPlanPrueba);

            entity.ToTable("tblPlanPrueba");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblPlanPrueba)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblPlanPrueba_tblProyecto");

            entity.HasOne(d => d.IdReleaseNavigation).WithMany(p => p.TblPlanPrueba)
                .HasForeignKey(d => d.IdRelease)
                .HasConstraintName("FK_tblPlanPrueba_tblRelease");
        });

        modelBuilder.Entity<TblPlantillaNotificacion>(entity =>
        {
            entity.HasKey(e => e.IdPlantillaNotificacion);

            entity.ToTable("tblPlantillaNotificacion");

            entity.HasIndex(e => new { e.Clave, e.Canal }, "UQ_tblPlantillaNotificacion_ClaveCanal").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Asunto).HasMaxLength(200);
            entity.Property(e => e.Canal).HasMaxLength(50);
            entity.Property(e => e.Clave).HasMaxLength(100);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblPortafolio>(entity =>
        {
            entity.HasKey(e => e.IdPortafolio);

            entity.ToTable("tblPortafolio");

            entity.HasIndex(e => e.Nombre, "UQ_tblPortafolio_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblPresupuestoProyecto>(entity =>
        {
            entity.HasKey(e => e.IdPresupuestoProyecto);

            entity.ToTable("tblPresupuestoProyecto");

            entity.HasIndex(e => new { e.IdProyecto, e.Anio }, "UQ_tblPresupuestoProyecto_ProyectoAnio").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.HorasAutorizadas).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MontoAutorizado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblPresupuestoProyecto)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblPresupuestoProyecto_tblProyecto");
        });

        modelBuilder.Entity<TblPrioridad>(entity =>
        {
            entity.ToTable("tblPrioridad");

            entity.HasIndex(e => e.Nombre, "UQ_tblPrioridad_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblProceso>(entity =>
        {
            entity.HasKey(e => e.IdProceso);

            entity.ToTable("tblProceso");

            entity.HasIndex(e => e.Proceso, "UQ_tblProceso_Proceso").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.ColumnaEstatus).HasMaxLength(128);
            entity.Property(e => e.ColumnaPk)
                .HasMaxLength(128)
                .HasColumnName("ColumnaPK");
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Proceso).HasMaxLength(100);
            entity.Property(e => e.TablaEstatus).HasMaxLength(300);
            entity.Property(e => e.TablaTransaccional).HasMaxLength(300);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblPrograma>(entity =>
        {
            entity.HasKey(e => e.IdPrograma);

            entity.ToTable("tblPrograma");

            entity.HasIndex(e => e.Nombre, "UQ_tblPrograma_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdPortafolioNavigation).WithMany(p => p.TblPrograma)
                .HasForeignKey(d => d.IdPortafolio)
                .HasConstraintName("FK_tblPrograma_tblPortafolio");
        });

        modelBuilder.Entity<TblProyecto>(entity =>
        {
            entity.HasKey(e => e.IdProyecto);

            entity.ToTable("tblProyecto");

            entity.HasIndex(e => e.Clave, "UQ_tblProyecto_Clave").IsUnique();

            entity.HasIndex(e => e.Folio, "UQ_tblProyecto_Folio")
                .IsUnique()
                .HasFilter("([Folio] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Clave).HasMaxLength(20);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Folio).HasMaxLength(50);
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdCategoriaProyectoNavigation).WithMany(p => p.TblProyecto)
                .HasForeignKey(d => d.IdCategoriaProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblProyecto_tblCategoriaProyecto");

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.TblProyecto)
                .HasForeignKey(d => d.IdEquipo)
                .HasConstraintName("FK_tblProyecto_tblEquipo");

            entity.HasOne(d => d.IdEstatusProyectoNavigation).WithMany(p => p.TblProyecto)
                .HasForeignKey(d => d.IdEstatusProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblProyecto_tblEstatusProyecto");

            entity.HasOne(d => d.IdProgramaNavigation).WithMany(p => p.TblProyecto)
                .HasForeignKey(d => d.IdPrograma)
                .HasConstraintName("FK_tblProyecto_tblPrograma");

            entity.HasOne(d => d.IdResponsableNavigation).WithMany(p => p.TblProyecto)
                .HasForeignKey(d => d.IdResponsable)
                .HasConstraintName("FK_tblProyecto_tblUsuario");
        });

        modelBuilder.Entity<TblPuesto>(entity =>
        {
            entity.HasKey(e => e.IdPuesto);

            entity.ToTable("tblPuesto");

            entity.HasIndex(e => e.Nombre, "UQ_tblPuesto_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdAreaNavigation).WithMany(p => p.TblPuesto)
                .HasForeignKey(d => d.IdArea)
                .HasConstraintName("FK_tblPuesto_tblArea");
        });

        modelBuilder.Entity<TblPullRequest>(entity =>
        {
            entity.HasKey(e => e.IdPullRequest);

            entity.ToTable("tblPullRequest");

            entity.HasIndex(e => new { e.IdRepositorio, e.Numero }, "UQ_tblPullRequest_RepositorioNumero").IsUnique();

            entity.Property(e => e.Autor).HasMaxLength(200);
            entity.Property(e => e.EstatusPr).HasMaxLength(20);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.RamaDestino).HasMaxLength(200);
            entity.Property(e => e.RamaOrigen).HasMaxLength(200);
            entity.Property(e => e.Titulo).HasMaxLength(200);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdRepositorioNavigation).WithMany(p => p.TblPullRequest)
                .HasForeignKey(d => d.IdRepositorio)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblPullRequest_tblRepositorio");

            entity.HasOne(d => d.IdWorkItemNavigation).WithMany(p => p.TblPullRequest)
                .HasForeignKey(d => d.IdWorkItem)
                .HasConstraintName("FK_tblPullRequest_tblWorkItem");
        });

        modelBuilder.Entity<TblRefreshToken>(entity =>
        {
            entity.HasKey(e => e.IdRefreshToken);

            entity.ToTable("tblRefreshToken");

            entity.HasIndex(e => e.IdUsuario, "IX_tblRefreshToken_Usuario");

            entity.HasIndex(e => new { e.IdUsuario, e.FechaExpiracion }, "IX_tblRefreshToken_Vigentes").HasFilter("([FechaRevocado] IS NULL)");

            entity.HasIndex(e => e.TokenHash, "UQ_tblRefreshToken_TokenHash").IsUnique();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IpOrigen).HasMaxLength(50);
            entity.Property(e => e.TokenHash).HasMaxLength(100);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdReemplazadoPorNavigation).WithMany(p => p.InverseIdReemplazadoPorNavigation)
                .HasForeignKey(d => d.IdReemplazadoPor)
                .HasConstraintName("FK_tblRefreshToken_tblRefreshToken");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblRefreshToken)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRefreshToken_tblUsuario");
        });

        modelBuilder.Entity<TblRegistroTiempo>(entity =>
        {
            entity.HasKey(e => e.IdRegistroTiempo);

            entity.ToTable("tblRegistroTiempo");

            entity.HasIndex(e => new { e.IdUsuario, e.Fecha }, "IX_tblRegistroTiempo_UsuarioFecha");

            entity.HasIndex(e => e.IdWorkItem, "IX_tblRegistroTiempo_WorkItem");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblRegistroTiempo)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRegistroTiempo_tblUsuario");

            entity.HasOne(d => d.IdWorkItemNavigation).WithMany(p => p.TblRegistroTiempo)
                .HasForeignKey(d => d.IdWorkItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRegistroTiempo_tblWorkItem");
        });

        modelBuilder.Entity<TblReglaAutomatizacion>(entity =>
        {
            entity.HasKey(e => e.IdReglaAutomatizacion);

            entity.ToTable("tblReglaAutomatizacion");

            entity.HasIndex(e => e.Nombre, "UQ_tblReglaAutomatizacion_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Evento).HasMaxLength(100);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblRelease>(entity =>
        {
            entity.HasKey(e => e.IdRelease);

            entity.ToTable("tblRelease");

            entity.HasIndex(e => e.Folio, "UQ_tblRelease_Folio")
                .IsUnique()
                .HasFilter("([Folio] IS NOT NULL)");

            entity.HasIndex(e => new { e.IdProyecto, e.Version }, "UQ_tblRelease_ProyectoVersion").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Folio).HasMaxLength(50);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
            entity.Property(e => e.Version).HasMaxLength(50);

            entity.HasOne(d => d.IdEstatusReleaseNavigation).WithMany(p => p.TblRelease)
                .HasForeignKey(d => d.IdEstatusRelease)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRelease_tblEstatusRelease");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblRelease)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRelease_tblProyecto");
        });

        modelBuilder.Entity<TblReleaseArtefacto>(entity =>
        {
            entity.HasKey(e => e.IdReleaseArtefacto);

            entity.ToTable("tblReleaseArtefacto");

            entity.HasIndex(e => new { e.IdRelease, e.IdArtefacto }, "UQ_tblReleaseArtefacto_ReleaseArtefacto").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.JustificacionIrreversible).HasMaxLength(500);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdArtefactoNavigation).WithMany(p => p.TblReleaseArtefactoIdArtefactoNavigation)
                .HasForeignKey(d => d.IdArtefacto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblReleaseArtefacto_tblArtefacto");

            entity.HasOne(d => d.IdArtefactoRollbackNavigation).WithMany(p => p.TblReleaseArtefactoIdArtefactoRollbackNavigation)
                .HasForeignKey(d => d.IdArtefactoRollback)
                .HasConstraintName("FK_tblReleaseArtefacto_tblArtefactoRollback");

            entity.HasOne(d => d.IdReleaseNavigation).WithMany(p => p.TblReleaseArtefacto)
                .HasForeignKey(d => d.IdRelease)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblReleaseArtefacto_tblRelease");
        });

        modelBuilder.Entity<TblRepositorio>(entity =>
        {
            entity.HasKey(e => e.IdRepositorio);

            entity.ToTable("tblRepositorio");

            entity.HasIndex(e => e.Url, "UQ_tblRepositorio_Url").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.SecretoWebhook).HasMaxLength(200);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblRepositorio)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRepositorio_tblProyecto");
        });

        modelBuilder.Entity<TblResultadoClave>(entity =>
        {
            entity.HasKey(e => e.IdResultadoClave);

            entity.ToTable("tblResultadoClave");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.ClaveKpi).HasMaxLength(100);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
            entity.Property(e => e.ValorActual).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorMeta).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.IdObjetivoOkrNavigation).WithMany(p => p.TblResultadoClave)
                .HasForeignKey(d => d.IdObjetivoOkr)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblResultadoClave_tblObjetivoOkr");
        });

        modelBuilder.Entity<TblResultadoPrueba>(entity =>
        {
            entity.ToTable("tblResultadoPrueba");

            entity.HasIndex(e => e.Nombre, "UQ_tblResultadoPrueba_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblRevision>(entity =>
        {
            entity.HasKey(e => e.IdRevision);

            entity.ToTable("tblRevision");

            entity.HasIndex(e => e.IdWorkItem, "IX_tblRevision_Pendientes").HasFilter("([Corregido]=(0))");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEstatusRevisionNavigation).WithMany(p => p.TblRevision)
                .HasForeignKey(d => d.IdEstatusRevision)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRevision_tblEstatusRevision");

            entity.HasOne(d => d.IdRevisorNavigation).WithMany(p => p.TblRevision)
                .HasForeignKey(d => d.IdRevisor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRevision_tblUsuario");

            entity.HasOne(d => d.IdWorkItemNavigation).WithMany(p => p.TblRevision)
                .HasForeignKey(d => d.IdWorkItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRevision_tblWorkItem");
        });

        modelBuilder.Entity<TblRiesgo>(entity =>
        {
            entity.HasKey(e => e.IdRiesgo);

            entity.ToTable("tblRiesgo");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Exposicion).HasComputedColumnSql("([Probabilidad]*[Impacto])", true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PlanMitigacion).HasMaxLength(500);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEstatusRiesgoNavigation).WithMany(p => p.TblRiesgo)
                .HasForeignKey(d => d.IdEstatusRiesgo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRiesgo_tblEstatusRiesgo");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblRiesgo)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRiesgo_tblProyecto");

            entity.HasOne(d => d.IdResponsableNavigation).WithMany(p => p.TblRiesgo)
                .HasForeignKey(d => d.IdResponsable)
                .HasConstraintName("FK_tblRiesgo_tblUsuario");
        });

        modelBuilder.Entity<TblRol>(entity =>
        {
            entity.HasKey(e => e.IdRol);

            entity.ToTable("tblRol");

            entity.HasIndex(e => e.Nombre, "UQ_tblRol_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblRolPermiso>(entity =>
        {
            entity.HasKey(e => e.IdRolPermiso);

            entity.ToTable("tblRolPermiso");

            entity.HasIndex(e => new { e.IdRol, e.IdPermiso }, "UQ_tblRolPermiso_RolPermiso").IsUnique();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdPermisoNavigation).WithMany(p => p.TblRolPermiso)
                .HasForeignKey(d => d.IdPermiso)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRolPermiso_tblPermiso");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.TblRolPermiso)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRolPermiso_tblRol");
        });

        modelBuilder.Entity<TblSeveridad>(entity =>
        {
            entity.ToTable("tblSeveridad");

            entity.HasIndex(e => e.Nombre, "UQ_tblSeveridad_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblSla>(entity =>
        {
            entity.HasKey(e => e.IdSla);

            entity.ToTable("tblSla");

            entity.HasIndex(e => e.Nombre, "UQ_tblSla_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdHorarioNavigation).WithMany(p => p.TblSla)
                .HasForeignKey(d => d.IdHorario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblSla_tblHorario");

            entity.HasOne(d => d.IdPrioridadNavigation).WithMany(p => p.TblSla)
                .HasForeignKey(d => d.IdPrioridad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblSla_tblPrioridad");
        });

        modelBuilder.Entity<TblSolicitud>(entity =>
        {
            entity.HasKey(e => e.IdSolicitud);

            entity.ToTable("tblSolicitud");

            entity.HasIndex(e => new { e.IdEstatusSolicitud, e.Activo }, "IX_tblSolicitud_Triage");

            entity.HasIndex(e => e.Folio, "UQ_tblSolicitud_Folio")
                .IsUnique()
                .HasFilter("([Folio] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Folio).HasMaxLength(50);
            entity.Property(e => e.JustificacionNegocio).HasMaxLength(500);
            entity.Property(e => e.Titulo).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEstatusSolicitudNavigation).WithMany(p => p.TblSolicitud)
                .HasForeignKey(d => d.IdEstatusSolicitud)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblSolicitud_tblEstatusSolicitud");

            entity.HasOne(d => d.IdPrioridadNavigation).WithMany(p => p.TblSolicitud)
                .HasForeignKey(d => d.IdPrioridad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblSolicitud_tblPrioridad");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblSolicitud)
                .HasForeignKey(d => d.IdProyecto)
                .HasConstraintName("FK_tblSolicitud_tblProyecto");

            entity.HasOne(d => d.IdSolicitanteNavigation).WithMany(p => p.TblSolicitud)
                .HasForeignKey(d => d.IdSolicitante)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblSolicitud_tblUsuario");

            entity.HasOne(d => d.IdTipoSolicitudNavigation).WithMany(p => p.TblSolicitud)
                .HasForeignKey(d => d.IdTipoSolicitud)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblSolicitud_tblTipoSolicitud");

            entity.HasOne(d => d.IdUsuarioSolicitanteNavigation).WithMany(p => p.TblSolicitud)
                .HasForeignKey(d => d.IdUsuarioSolicitante)
                .HasConstraintName("FK_tblSolicitud_tblUsuarioSolicitanteCatalogo");
        });

        modelBuilder.Entity<TblSprint>(entity =>
        {
            entity.HasKey(e => e.IdSprint);

            entity.ToTable("tblSprint");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Objetivo).HasMaxLength(500);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.TblSprint)
                .HasForeignKey(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblSprint_tblEquipo");

            entity.HasOne(d => d.IdEstatusSprintNavigation).WithMany(p => p.TblSprint)
                .HasForeignKey(d => d.IdEstatusSprint)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblSprint_tblEstatusSprint");
        });

        modelBuilder.Entity<TblTablero>(entity =>
        {
            entity.HasKey(e => e.IdTablero);

            entity.ToTable("tblTablero");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.TblTablero)
                .HasForeignKey(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblTablero_tblEquipo");
        });

        modelBuilder.Entity<TblTableroColumna>(entity =>
        {
            entity.HasKey(e => e.IdTableroColumna);

            entity.ToTable("tblTableroColumna");

            entity.HasIndex(e => new { e.IdTablero, e.IdEstatusWorkItem }, "UQ_tblTableroColumna_TableroEstatus").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEstatusWorkItemNavigation).WithMany(p => p.TblTableroColumna)
                .HasForeignKey(d => d.IdEstatusWorkItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblTableroColumna_tblEstatusWorkItem");

            entity.HasOne(d => d.IdTableroNavigation).WithMany(p => p.TblTableroColumna)
                .HasForeignKey(d => d.IdTablero)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblTableroColumna_tblTablero");
        });

        modelBuilder.Entity<TblTarifaNivel>(entity =>
        {
            entity.HasKey(e => e.IdTarifaNivel);

            entity.ToTable("tblTarifaNivel");

            entity.HasIndex(e => new { e.IdNivel, e.VigenciaDesde }, "UQ_tblTarifaNivel_NivelVigencia").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CostoHora).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdNivelNavigation).WithMany(p => p.TblTarifaNivel)
                .HasForeignKey(d => d.IdNivel)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblTarifaNivel_tblNivel");
        });

        modelBuilder.Entity<TblTicket>(entity =>
        {
            entity.HasKey(e => e.IdTicket);

            entity.ToTable("tblTicket");

            entity.HasIndex(e => new { e.IdAsignado, e.IdEstatusTicket }, "IX_tblTicket_Asignado");

            entity.HasIndex(e => new { e.IdEstatusTicket, e.FechaLimiteResolucion }, "IX_tblTicket_SlaVigilancia").HasFilter("([FechaResolucion] IS NULL)");

            entity.HasIndex(e => e.Folio, "UQ_tblTicket_Folio")
                .IsUnique()
                .HasFilter("([Folio] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Folio).HasMaxLength(50);
            entity.Property(e => e.Titulo).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdAsignadoNavigation).WithMany(p => p.TblTicketIdAsignadoNavigation)
                .HasForeignKey(d => d.IdAsignado)
                .HasConstraintName("FK_tblTicket_tblUsuarioAsignado");

            entity.HasOne(d => d.IdCategoriaTicketNavigation).WithMany(p => p.TblTicket)
                .HasForeignKey(d => d.IdCategoriaTicket)
                .HasConstraintName("FK_tblTicket_tblCategoriaTicket");

            entity.HasOne(d => d.IdEstatusTicketNavigation).WithMany(p => p.TblTicket)
                .HasForeignKey(d => d.IdEstatusTicket)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblTicket_tblEstatusTicket");

            entity.HasOne(d => d.IdPrioridadNavigation).WithMany(p => p.TblTicket)
                .HasForeignKey(d => d.IdPrioridad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblTicket_tblPrioridad");

            entity.HasOne(d => d.IdSlaNavigation).WithMany(p => p.TblTicket)
                .HasForeignKey(d => d.IdSla)
                .HasConstraintName("FK_tblTicket_tblSla");

            entity.HasOne(d => d.IdSolicitanteNavigation).WithMany(p => p.TblTicketIdSolicitanteNavigation)
                .HasForeignKey(d => d.IdSolicitante)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblTicket_tblUsuarioSolicitante");

            entity.HasOne(d => d.IdWorkItemDerivadoNavigation).WithMany(p => p.TblTicket)
                .HasForeignKey(d => d.IdWorkItemDerivado)
                .HasConstraintName("FK_tblTicket_tblWorkItem");

            entity.HasOne(d => d.IdUsuarioSolicitanteNavigation).WithMany(p => p.TblTicket)
                .HasForeignKey(d => d.IdUsuarioSolicitante)
                .HasConstraintName("FK_tblTicket_tblUsuarioSolicitanteCatalogo");

            entity.HasOne(d => d.IdLocacionNavigation).WithMany(p => p.TblTicket)
                .HasForeignKey(d => d.IdLocacion)
                .HasConstraintName("FK_tblTicket_tblLocacion");
        });

        modelBuilder.Entity<TblTipoArtefacto>(entity =>
        {
            entity.ToTable("tblTipoArtefacto");

            entity.HasIndex(e => e.Nombre, "UQ_tblTipoArtefacto_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblTipoAusencia>(entity =>
        {
            entity.ToTable("tblTipoAusencia");

            entity.HasIndex(e => e.Nombre, "UQ_tblTipoAusencia_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblTipoPrueba>(entity =>
        {
            entity.ToTable("tblTipoPrueba");

            entity.HasIndex(e => e.Nombre, "UQ_tblTipoPrueba_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblTipoSolicitud>(entity =>
        {
            entity.ToTable("tblTipoSolicitud");

            entity.HasIndex(e => e.Nombre, "UQ_tblTipoSolicitud_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblTipoVinculo>(entity =>
        {
            entity.ToTable("tblTipoVinculo");

            entity.HasIndex(e => e.Nombre, "UQ_tblTipoVinculo_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblTipoWorkItem>(entity =>
        {
            entity.ToTable("tblTipoWorkItem");

            entity.HasIndex(e => e.Nombre, "UQ_tblTipoWorkItem_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblTransicion>(entity =>
        {
            entity.HasKey(e => e.IdTransicion);

            entity.ToTable("tblTransicion");

            entity.HasIndex(e => new { e.IdProceso, e.IdEstatusOrigen, e.Accion }, "UQ_tblTransicion_ProcesoOrigenAccion").IsUnique();

            entity.Property(e => e.Accion).HasMaxLength(50);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdProcesoNavigation).WithMany(p => p.TblTransicion)
                .HasForeignKey(d => d.IdProceso)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblTransicion_tblProceso");
        });

        modelBuilder.Entity<TblTransicionConfig>(entity =>
        {
            entity.HasKey(e => e.IdTransicionConfig);

            entity.ToTable("tblTransicionConfig");

            entity.HasIndex(e => new { e.Proceso, e.IdEstatusOrigen, e.Accion }, "UQ_tblTransicionConfig_ProcesoOrigenAccion").IsUnique();

            entity.Property(e => e.Accion).HasMaxLength(50);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.EtiquetaBoton).HasMaxLength(100);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IconoAccion).HasMaxLength(50);
            entity.Property(e => e.Proceso).HasMaxLength(50);
            entity.Property(e => e.RequierePermiso).HasMaxLength(100);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblUsuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.ToTable("tblUsuario");

            entity.HasIndex(e => e.IdJefe, "IX_tblUsuario_Jefe").HasFilter("([IdJefe] IS NOT NULL)");

            entity.HasIndex(e => e.Dominio, "UQ_tblUsuario_Dominio").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Correo).HasMaxLength(200);
            entity.Property(e => e.Dominio).HasMaxLength(100);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IntentosFallidos).HasDefaultValue(0);
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.PasswordHash).HasMaxLength(200);
            entity.Property(e => e.RequiereCambioPassword).HasDefaultValue(true);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdHorarioNavigation).WithMany(p => p.TblUsuario)
                .HasForeignKey(d => d.IdHorario)
                .HasConstraintName("FK_tblUsuario_tblHorario");

            entity.HasOne(d => d.IdJefeNavigation).WithMany(p => p.InverseIdJefeNavigation)
                .HasForeignKey(d => d.IdJefe)
                .HasConstraintName("FK_tblUsuario_tblUsuario");

            entity.HasOne(d => d.IdNivelNavigation).WithMany(p => p.TblUsuario)
                .HasForeignKey(d => d.IdNivel)
                .HasConstraintName("FK_tblUsuario_tblNivel");

            entity.HasOne(d => d.IdPuestoNavigation).WithMany(p => p.TblUsuario)
                .HasForeignKey(d => d.IdPuesto)
                .HasConstraintName("FK_tblUsuario_tblPuesto");
        });

        modelBuilder.Entity<TblUsuarioRol>(entity =>
        {
            entity.HasKey(e => e.IdUsuarioRol);

            entity.ToTable("tblUsuarioRol");

            entity.HasIndex(e => e.IdUsuario, "IX_tblUsuarioRol_Usuario");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.TblUsuarioRol)
                .HasForeignKey(d => d.IdEquipo)
                .HasConstraintName("FK_tblUsuarioRol_tblEquipo");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblUsuarioRol)
                .HasForeignKey(d => d.IdProyecto)
                .HasConstraintName("FK_tblUsuarioRol_tblProyecto");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.TblUsuarioRol)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblUsuarioRol_tblRol");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblUsuarioRol)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblUsuarioRol_tblUsuario");
        });

        modelBuilder.Entity<TblUsuarioSolicitante>(entity =>
        {
            entity.HasKey(e => e.IdUsuarioSolicitante);

            entity.ToTable("tblUsuarioSolicitante");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Correo).HasMaxLength(150);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(500);
            entity.Property(e => e.Usuario).HasMaxLength(50);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
        });

        modelBuilder.Entity<TblVersionSistema>(entity =>
        {
            entity.HasKey(e => e.IdVersionSistema);

            entity.ToTable("tblVersionSistema");

            entity.HasIndex(e => e.Version, "UQ_tblVersionSistema_Version").IsUnique();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);
            entity.Property(e => e.Version).HasMaxLength(50);
        });

        modelBuilder.Entity<TblWorkItem>(entity =>
        {
            entity.HasKey(e => e.IdWorkItem);

            entity.ToTable("tblWorkItem", tb => tb.HasTrigger("trWorkItemHistorialCampo"));

            entity.HasIndex(e => new { e.IdAsignado, e.IdEstatusWorkItem, e.Activo }, "IX_tblWorkItem_Bandeja");

            entity.HasIndex(e => e.IdEquipo, "IX_tblWorkItem_Equipo");

            entity.HasIndex(e => e.IdPadre, "IX_tblWorkItem_Padre").HasFilter("([IdPadre] IS NOT NULL)");

            entity.HasIndex(e => new { e.IdProyecto, e.IdEstatusWorkItem }, "IX_tblWorkItem_Proyecto");

            entity.HasIndex(e => e.IdSprint, "IX_tblWorkItem_Sprint").HasFilter("([IdSprint] IS NOT NULL)");

            entity.HasIndex(e => e.ClaveJira, "UQ_tblWorkItem_ClaveJira")
                .IsUnique()
                .HasFilter("([ClaveJira] IS NOT NULL)");

            entity.HasIndex(e => e.Folio, "UQ_tblWorkItem_Folio").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.ClaveJira).HasMaxLength(50);
            entity.Property(e => e.FechaMovto).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Folio).HasMaxLength(50);
            entity.Property(e => e.Locacion).HasMaxLength(100);
            entity.Property(e => e.PuntosHistoria).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.Titulo).HasMaxLength(200);
            entity.Property(e => e.UsuarioMovto).HasMaxLength(50);
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdAsignadoNavigation).WithMany(p => p.TblWorkItemIdAsignadoNavigation)
                .HasForeignKey(d => d.IdAsignado)
                .HasConstraintName("FK_tblWorkItem_tblUsuarioAsignado");

            entity.HasOne(d => d.IdComplejidadNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdComplejidad)
                .HasConstraintName("FK_tblWorkItem_tblComplejidad");

            entity.HasOne(d => d.IdEjecucionPruebaOrigenNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdEjecucionPruebaOrigen)
                .HasConstraintName("FK_tblWorkItem_tblEjecucionPrueba");

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdEquipo)
                .HasConstraintName("FK_tblWorkItem_tblEquipo");

            entity.HasOne(d => d.IdEstatusWorkItemNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdEstatusWorkItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblWorkItem_tblEstatusWorkItem");

            entity.HasOne(d => d.IdPadreNavigation).WithMany(p => p.InverseIdPadreNavigation)
                .HasForeignKey(d => d.IdPadre)
                .HasConstraintName("FK_tblWorkItem_tblWorkItem");

            entity.HasOne(d => d.IdPrioridadNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdPrioridad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblWorkItem_tblPrioridad");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblWorkItem_tblProyecto");

            entity.HasOne(d => d.IdReleaseNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdRelease)
                .HasConstraintName("FK_tblWorkItem_tblRelease");

            entity.HasOne(d => d.IdSolicitanteNavigation).WithMany(p => p.TblWorkItemIdSolicitanteNavigation)
                .HasForeignKey(d => d.IdSolicitante)
                .HasConstraintName("FK_tblWorkItem_tblUsuarioSolicitante");

            entity.HasOne(d => d.IdSolicitudNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdSolicitud)
                .HasConstraintName("FK_tblWorkItem_tblSolicitud");

            entity.HasOne(d => d.IdSprintNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdSprint)
                .HasConstraintName("FK_tblWorkItem_tblSprint");

            entity.HasOne(d => d.IdTipoWorkItemNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdTipoWorkItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblWorkItem_tblTipoWorkItem");

            entity.HasOne(d => d.IdUsuarioSolicitanteNavigation).WithMany(p => p.TblWorkItem)
                .HasForeignKey(d => d.IdUsuarioSolicitante)
                .HasConstraintName("FK_tblWorkItem_tblUsuarioSolicitanteCatalogo");
        });

        modelBuilder.Entity<TblWorkItemVinculo>(entity =>
        {
            entity.HasKey(e => e.IdWorkItemVinculo);

            entity.ToTable("tblWorkItemVinculo");

            entity.HasIndex(e => new { e.IdWorkItemOrigen, e.IdWorkItemDestino, e.IdTipoVinculo }, "UQ_tblWorkItemVinculo_OrigenDestinoTipo").IsUnique();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioRegistro).HasMaxLength(200);

            entity.HasOne(d => d.IdTipoVinculoNavigation).WithMany(p => p.TblWorkItemVinculo)
                .HasForeignKey(d => d.IdTipoVinculo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblWorkItemVinculo_tblTipoVinculo");

            entity.HasOne(d => d.IdWorkItemDestinoNavigation).WithMany(p => p.TblWorkItemVinculoIdWorkItemDestinoNavigation)
                .HasForeignKey(d => d.IdWorkItemDestino)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblWorkItemVinculo_tblWorkItemDestino");

            entity.HasOne(d => d.IdWorkItemOrigenNavigation).WithMany(p => p.TblWorkItemVinculoIdWorkItemOrigenNavigation)
                .HasForeignKey(d => d.IdWorkItemOrigen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblWorkItemVinculo_tblWorkItemOrigen");
        });

        modelBuilder.Entity<VwBandejaTrabajo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBandejaTrabajo");

            entity.Property(e => e.Asignado).HasMaxLength(200);
            entity.Property(e => e.ClaveProyecto).HasMaxLength(20);
            entity.Property(e => e.Estatus).HasMaxLength(100);
            entity.Property(e => e.Folio).HasMaxLength(50);
            entity.Property(e => e.Prioridad).HasMaxLength(100);
            entity.Property(e => e.Proyecto).HasMaxLength(200);
            entity.Property(e => e.PuntosHistoria).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.Solicitante).HasMaxLength(200);
            entity.Property(e => e.Sprint).HasMaxLength(100);
            entity.Property(e => e.Tipo).HasMaxLength(100);
            entity.Property(e => e.Titulo).HasMaxLength(200);
        });

        modelBuilder.Entity<VwTiempoInvertido>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwTiempoInvertido");
        });

        modelBuilder.Entity<VwCostoRegistroTiempo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwCostoRegistroTiempo");

            entity.Property(e => e.CostoHora).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Costo).HasColumnType("decimal(18, 4)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
